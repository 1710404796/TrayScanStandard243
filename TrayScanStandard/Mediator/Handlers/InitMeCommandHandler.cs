
using Humanizer;
using LinxUniverse.Auth;
using LinxUniverse.CST;
using LinxUniverse.Utils;
using LinxUniverse.VM;
using MediatR;
using Microsoft.Extensions.Logging;
using MugenCodeDetecter;
using RPDelectPallet.Meditor.Queries;
using System.Data;
using System.Text;
using System.Windows;
using TrayScanStandard.Attritubes;
using TrayScanStandard.Data;
using TrayScanStandard.Mediator.Commands;
using TrayScanStandard.Models.CZPallet;
using TrayScanStandard.Service;
using VMWebAIClient;

namespace TrayScanStandard.Mediator.Handlers
{
    /// <summary>
    /// 初始化命令处理器
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="logger"></param>
    /// <param name="role"></param>
    public class InitMeCommandHandler(IMediator mediator,
        ILogger<InitMeCommandHandler> logger, 
        RoleManager<LinxRole, LinxUser> role
        , ScanCameraService scanCameraService
        , LinxContext linxContext
        , IVMWebAIClient vmWebAIClient
        )
        : IRequestHandler<InitMeCommand>
    {
        public async Task Handle(InitMeCommand request, CancellationToken cancellationToken)
        {
            FilenameHelper.CreateDir("DataFrame");
            FilenameHelper.CreateDir("InsertLog");
            FilenameHelper.CreateDir("Data2D");

            foreach (var item in Enum.GetValues<PowerEnum>())
            {
                if (!MainStorage.Saves.PowerTable.ContainsKey(item))
                {
                    MainStorage.Saves.PowerTable[item] = [];
                }

                foreach (var item1 in Enum.GetValues<RoleEnum>())
                {
                    if (!MainStorage.Saves.PowerTable[item].ContainsKey(item1))
                    {
                        MainStorage.Saves.PowerTable[item][item1] = false;
                    }
                }
            }
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            MainStorage.SelectBattery = linxContext.BatteryTypeInfos.FirstOrDefault(s => s.Id == MainStorage.Saves.SelectBatteryId);

            foreach (var item in Enum.GetNames<RoleEnum>())
            {

                await role.CreateAsync(new LinxRole { RoleName = item });
            }
            scanCameraService.Init();
            // 通常这里要初始化一些硬件设备

            // 例如：相机、光源、CST等

            // 也需要注册一些任务等
            MainStorage.CST = await MainStorage.Saves.LightInfos.Map(
                async s =>
                {
                    if (!Enum.TryParse<SerialPortType>(s.Com, true, out var com))
                    {
                        throw new Exception($"光源地址 '{s.Com}' 不是有效的 COM 端口（支持 COM1-COM8）");
                    }

                    // 根据光源类型选择底层驱动：
                    // Cognex -> CSTControllerDll
                    // Wordop -> 串口 ASCII 协议
                    var controllerType = s.Type == LightType.Wordop
                        ? LightControllerType.Wordop
                        : LightControllerType.Cognex;

                    // Wordop: 19200, Cognex: 9600
                    var baudRate = s.Type == LightType.Wordop ? 19200 : 9600;

                    var g = await mediator.Send(new CreateCSTLightCommand(
                        Com: com,
                        ControllerType: controllerType,
                        BaudRate: baudRate));
                    return await mediator.Send(new GetLightQuery(g));
                }).TraverseSerial(s => s!);
            //var sol= CodeDetectExtensions
            //    .LoadSolution(new VMSolutionInfo(@"test.sol", ""));
            //Console.WriteLine(sol);
            //MainStorage.Algo = sol
            //    .Bind(s => s.CreateAlgo(new DetectVMConfig("test", "legacy_detect")))
            //    ;
            //MainStorage.AlgoCnn = sol
            //    .Bind(s => s.CreateAlgo(new DetectVMCnnConfig("test", "cnn_detect")));
            var algores = await vmWebAIClient.CreateAlgoAsync(FilenameHelper.AppPath + @"test.sol", LinxUniverse.Algo.Common.DetectType.VisionMaster);

            logger.LogInformation("算法加载结果: {Result}", algores);


            if (MainStorage.CST == null) 
            { 
                logger.LogError("光源初始化失败");
                MessageBox.Show("光源初始化失败", "光源初始化失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            algores.IfLeft(
                s =>
                {
                    logger.LogError(s);
                    MessageBox.Show("算法加载错误\n" + s, "算法加载错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            );
            MainStorage.Algo.IfLeft(
                s =>
                {
                    logger.LogError(s);
                    MessageBox.Show("算法加载错误\n" + s, "算法加载错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            );
            MainStorage.AlgoCnn.IfLeft(
              s =>
              {
                  logger.LogError(s);
                  MessageBox.Show("cnn算法加载错误\n" + s, "算法加载错误", MessageBoxButton.OK, MessageBoxImage.Error);
                  return;
              }
          );

            //messageboxmanager
            //MainStorage.Cst[0] =
            //    await mediator.Send(new CreateCSTLightCommand(Com: SerialPortType.COM1));
            //if (string.IsNullOrEmpty(MainStorage.Saves.ExtCamKey))
            //{
            //    logger.LogWarning("广角镜头序列码未设定");
            //    return;
            //}
            //MainStorage.CamId = await mediator.Send(new CreateCameraCommand(new CameraDeciveInfo
            //{
            //    CameraType = CameraType.HK2d,
            //    Key = MainStorage.Saves.ExtCamKey,

            //}));

            //if (MainStorage.CamId != null)
            //{
            //    var cam = await mediator.Send(new GetCameraQuery(MainStorage.CamId)) as HKGigeTriggerCamera;
            //    cam.InitCamera();
            //    // todo: 曝光是否要标准化
            //    cam.SetExposureTime(MainStorage.Saves.ExtCamExposure);
            //    MainStorage.Cam = cam; 
            //}
            //else
            //{
            //    await mediator.Send(new WarningBoxCommand("广角相机初始化失败", SaveLog: false));
            //}
            //MainStorage.Cst[1] =
            //await mediator.Send(new CreateCSTLightCommand(Com: SerialPortType.COM2));

        }
    }
}
