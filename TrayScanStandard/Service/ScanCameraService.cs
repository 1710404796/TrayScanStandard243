using Camera.Fs.Common;
using HKCamera.Fs.NET;
using HKCamera.Fs.NET.Controls;
using LanguageExt;
using LinxUniverse.DI;
using MediatR;
using Microsoft.Extensions.Logging;
using MugenCamera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using TrayScanStandard.ViewModel;
using static MvCamCtrl.NET.MyCamera;
namespace TrayScanStandard.Service
{
    /// <summary>
    /// 相机服务，负责管理相机连接和状态监控
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="logger"></param>
    /// <param name="cacheService">缓存服务</param>
    public class ScanCameraService(IMediator mediator, 
        ILogger<ScanCameraService> logger, 
        CacheService cacheService)
    {
        public Option<MugenCamera.MugenCamera>[] MugenCameras { get; set; } = [];
        private Thread _listenThread;
        private readonly object _initLock = new();
        public BcrBorderViewModel[] BcrBorderViewModels = [];
        public Image2DViewModel[] Image2DViewModels = [];

        /// <summary>
        /// 初始化相机服务
        /// </summary>
        public void Init()
        {
            // 读取相机配置，初始化相机连接  按 相机总数 截取配置
            var settings = MainStorage.Saves.ConnectAddresses.Take(MainStorage.Saves.CameraCount);

            // 初始化相机连接，并记录结果   按配置逐个连接相机
            var cameras = settings.Map(setting => InitCamera(setting)).ToArray();

            // 保存连接结果
            // Either->Option，成功 Some(camera)，失败 None 存入 MugenCameras
            MugenCameras = [.. cameras.Map(camera => camera.ToOption())];

            // 初始化相机视图模型
            Image2DViewModels = [.. settings.Map((i, s) => new Image2DViewModel() {
                CameraSetting = s,
                CameraIdx = i + 1,
                Service = this
            })];

            // 初始化边框视图模型，绑定相机连接状态
            BcrBorderViewModels = [.. Image2DViewModels.Map(s => new BcrBorderViewModel() { Image2DViewModel = s })];

            // 启动监听线程，定期检查相机连接状态并尝试重连
            _listenThread = new Thread(Listen);
            _listenThread.Start();

            //MugenCameras = cameras.Match
        }

        // 获取指定索引的相机实例
        public Option< MugenCamera.MugenCamera> GetMugen(int idx) => MugenCameras[idx - 1];
        

        private static Either<string, MugenCamera.MugenCamera> InitCamera(TrayScanStandard.Models.CameraSetting s)
        {
            // 根据地址创建相机对象
            return s.CameraAddresses.Create()
                                .Bind(MugenCameraExtensions.Connect)        // 建立连接
                                .Bind(s =>
                                {
                                    // 设置采集触发方式（按相机类型）
                                    switch (s)
                                    {
                                        case HikVision:
                                           return s.SetControl(new AcquisitionControl
                                            {
                                                TriggerMode = MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON,
                                                TriggerSource = MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE
                                            });
                                        case HuaruiCam:
                                            return s.SetControl(new HuaRui.Fs.NET.Controls.AcquisitionControl
                                            {
                                                TriggerMode = HuaRui.Fs.NET.Controls.TriggerModel.On,
                                                TriggerSource = HuaRui.Fs.NET.Controls.TriggerSource.Software
                                            });
                                        default:
                                        return Right(s);

                                    };

                                    //return s.SetControl(c);
                                })
                                .Bind(s => s.SetControl(new
                                {    
                                    // 设置一下心跳
                                    GevHeartbeatTimeout = (long?)5000
                                }));
        }

        /// <summary>
        /// 单相机重连
        /// </summary>
        /// <param name="cameraIdx"></param>
        /// <returns></returns>
        public Either<string, bool> ReconnectCamera(int cameraIdx)
        {
            lock (_initLock)
            {
                if (cameraIdx <= 0 || cameraIdx > MainStorage.Saves.CameraCount)
                {
                    return Left($"相机索引越界: {cameraIdx}");
                }

                int arrayIdx = cameraIdx - 1;

                var setting = MainStorage.Saves.ConnectAddresses[arrayIdx];

                var result = InitCameraWithLog(setting, cameraIdx, "manual_reconnect");
                return result.Match(
                    Right: camera =>
                    {
                        if (arrayIdx < MugenCameras.Length)
                        {
                            MugenCameras[arrayIdx] = Some(camera);
                        }

                        if (arrayIdx < BcrBorderViewModels.Length)
                        {
                            BcrBorderViewModels[arrayIdx].IsConnect = camera.IsConnect();
                        }

                        logger.LogInformation($"手动重连相机[{cameraIdx}]成功");
                        return Right<string, bool>(true);
                    },
                    Left: err =>
                    {
                        if (arrayIdx < MugenCameras.Length)
                        {
                            MugenCameras[arrayIdx] = Option<MugenCamera.MugenCamera>.None;
                        }

                        if (arrayIdx < BcrBorderViewModels.Length)
                        {
                            BcrBorderViewModels[arrayIdx].IsConnect = false;
                        }

                        logger.LogWarning($"手动重连相机[{cameraIdx}]失败: {err}");
                        return Left<string, bool>(err);
                    });
            }
        }

        /// <summary>
        ///  连接状态维护与自动重连
        /// </summary>
        async void Listen()
        {
            while (!cacheService.Token.IsCancellationRequested)
            {
                MugenCameras = MugenCameras.Map((i, s) =>
                {
                    // 对每个 Some(camera) 调检查连接 CheckConnect()
                    return s.Bind(some => some.CheckConnect().ToOption());
                }).ToArray();

                MugenCameras = MugenCameras.Map((i, s) =>
                {
                    var address = MainStorage.Saves.ConnectAddresses[i];
                    return s.Match(
                        Some: s =>
                        {
                            if (s.IsConnect())
                            {
                                return Right(s);
                            }
                            else
                            {
                                // 若相机断开或当前是 `None`，调用 `InitCamera(address)` 重连
                                return InitCamera(address);
                            }
                        },
                        None: () =>
                        {   
                            // 若相机断开或当前是 `None`，调用 `InitCamera(address)` 重连
                            return InitCamera(address);
                        }).ToOption();
                }).ToArray();

                MugenCameras.Iter((i, s) =>
                {
                    // 把结果映射到 `BcrBorderViewModels[i].IsConnect`
                    var vm = BcrBorderViewModels[i];

                    vm.IsConnect = s.Match(
                        Some: s =>
                        {   // IsConnect=true 显示绿色边框和“Connected”，
                            return s.IsConnect();
                        },
                        None: () =>
                        {   
                            // IsConnect = false显示红色边框和“Disconnected”
                            return false;
                        });
                });

                // 每 10 秒巡检一次
                await Task.Delay(10000);
            }
        }


        private Either<string, MugenCamera.MugenCamera> InitCameraWithLog(TrayScanStandard.Models.CameraSetting setting, int cameraIdx, string stage)
        {
            var addressText = GetAddressText(setting);
            //if (string.IsNullOrWhiteSpace(addressText))
            //{
            //    logger.LogWarning("相机[{CameraIdx}]连接参数为空，阶段: {Stage}", cameraIdx, stage);
            //}

            var result = InitCamera(setting);
            result.Match(
                Right: _ =>
                {
                    logger.LogInformation($"相机[{cameraIdx}]连接成功", string.IsNullOrWhiteSpace(addressText) ? "<empty>" : addressText);
                    return 0;
                },
                Left: error =>
                {
                    logger.LogWarning($"相机[{cameraIdx}]连接失败，错误: {error}", string.IsNullOrWhiteSpace(addressText) ? "<empty>" : addressText, error);
                    return 0;
                });

            return result;
        }


        private static string GetAddressText(TrayScanStandard.Models.CameraSetting setting)
        {
            return setting.CameraAddresses switch
            {
                HKAddress hk => hk.ConnectAddress switch
                {
                    Camera.Fs.Common.Key key => key.Value,
                    Camera.Fs.Common.IPAddress ip => ip.Value,
                    Camera.Fs.Common.Serial serial => serial.Value,
                    _ => string.Empty
                },
                HuaruiAddress hr => hr.ConnectAddress switch
                {
                    Camera.Fs.Common.Key key => key.Value,
                    Camera.Fs.Common.IPAddress ip => ip.Value,
                    Camera.Fs.Common.Serial serial => serial.Value,
                    _ => string.Empty
                },
                _ => string.Empty
            };
        }
    }
}
