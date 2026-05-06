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
using TrayScanStandard.ViewModel;
using static MvCamCtrl.NET.MyCamera;
namespace TrayScanStandard.Service
{
    public class ScanCameraService(IMediator mediator, 
        ILogger<ScanCameraService> logger, 
        CacheService cacheService)
    {
        public Option<MugenCamera.MugenCamera>[] MugenCameras { get; set; } = [];
        private Thread _listenThread;
        public BcrBorderViewModel[] BcrBorderViewModels = [];
        public Image2DViewModel[] Image2DViewModels = [];
        public void Init()
        {
            var settings = MainStorage.Saves.ConnectAddresses
                .Take(MainStorage.Saves.CameraCnt);
            var cameras =
                settings
                .Map(s =>
                    InitCamera(s)
                ).ToArray()
                // Traverse to ensure all cameras are connected
                ;
            MugenCameras = [.. cameras.Map(
                c => c.ToOption()
                )];

            Image2DViewModels = [.. settings.Map((i, s) => new Image2DViewModel() {
                CameraSetting = s,
                CameraIdx = i + 1,
                Service = this

            })];

            BcrBorderViewModels =
                [.. Image2DViewModels.Map(s => new BcrBorderViewModel() { Image2DViewModel = s })];
            _listenThread = new Thread(Listen);
            _listenThread.Start();

            //MugenCameras = cameras.Match
        }

        public Option< MugenCamera.MugenCamera> GetMugen(int idx) => MugenCameras[idx - 1];

        private static Either<string, MugenCamera.MugenCamera> InitCamera(TrayScanStandard.Models.CameraSetting s)
        {
            return s.CameraAddresses.Create()
                                .Bind(MugenCameraExtensions.Connect)
                                .Bind(s =>
                                {
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

                                    }
                                    ;


                                    //return s.SetControl(c);
                                })
                                // 设置一下心跳
                                .Bind(s => s.SetControl(new
                                {
                                    GevHeartbeatTimeout = (long?)5000
                                }))
                                ;
        }
        async void Listen()
        {
            while (!cacheService.Token.IsCancellationRequested)
            {
                MugenCameras = MugenCameras.Map((i, s) =>
                {
                    return s.Bind(s => s.CheckConnect().ToOption());
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
                                return InitCamera(address);
                            }
                        },
                        None: () =>
                        {
                            return InitCamera(address);
                        }).ToOption();
                }).ToArray();

                MugenCameras.Iter((i, s) =>
                {
                    var vm = BcrBorderViewModels[i];

                    vm.IsConnect = s.Match(
                        Some: s =>
                        {
                            return s.IsConnect();
                        },
                        None: () =>
                        {
                            return false;
                        });
                });


                await Task.Delay(10000);
            }
        }

    }
}
