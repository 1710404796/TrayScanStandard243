

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TrayScanStandard.Data;
using TrayScanStandard.Data.Models;
using TrayScanStandard.Mediator.Commands;
using TrayScanStandard.ViewModel;

namespace TrayScanStandard.Apis
{
    [ApiController]
    [Route("[controller]")]
    public class WcsController(
        LinxContext linxContext, 
        IMediator mediator, 
        ILogger<WcsController> logger
        , MainViewModel mainViewModel
        ): ControllerBase
    {
        //[HttpGet("/CaptureImage")] 北京时代
        //[HttpPost("/Photo")] 福鼎时代
        private const string ERROR = "ERROR";
        private static readonly TimeSpan Rephotograph = TimeSpan.FromSeconds(3);

        [HttpPost("/CaptureImage/{port1}")]
        public async Task<QRCodeResult> Delect(string port1)
        {
            while (!mainViewModel.IsWcsEnable)
            {
                await Task.Delay(1000);
            }
            logger.LogInformation("收到检测任务");

            if (MainStorage.SelectBattery is null) return new QRCodeResult() { ErrorCode = ErrorType.ERROR };

            var batteryTypeInfo = MainStorage.SelectBattery;
            var cancellationToken = HttpContext.RequestAborted;
            var data = await mediator.Send(new DetectCCDCommand(batteryTypeInfo), cancellationToken);

            var decodedCount = data.Match(
                Right: result => result.Channels.Count(code =>
                    !string.IsNullOrWhiteSpace(code.Code) && code.Code != ERROR),
                Left: _ => batteryTypeInfo.Count);

            if (decodedCount < batteryTypeInfo.Count)
            {
                logger.LogInformation(
                    "本次解码数量为 {DecodedCount}/{ExpectedCount}，3秒后重新拍照并上传WCS",
                    decodedCount,
                    batteryTypeInfo.Count);

                await Task.Delay(Rephotograph, cancellationToken);
                data = await mediator.Send(new DetectCCDCommand(batteryTypeInfo), cancellationToken);
            }

            GC.Collect();
            data.IfRight(
                r =>
                {
                    var codes = r.Channels.ToDictionary(s => s.Index, s => s.Code);
                    var batteryCodes = Enumerable.Range(1, batteryTypeInfo.Count)
                        .Select(s => codes.GetValueOrDefault(s, ERROR))
                        .ToList();
                    var log = new PalletLog
                    {
                        Column = batteryTypeInfo.Column, // 这里要可派之
                        PalletType = Models.CZPallet.PalletType.组盘,

                        ChannelCount = batteryTypeInfo.Count,
                        BatteryInfo
                            = batteryCodes.Map(s => new BatteryInfo
                            {
                                BatteryCode = s,
                                BatteryLevel = string.IsNullOrEmpty(s) ? Models.BatteryLevel.ERROR : Models.BatteryLevel.OK
                            })
                            .ToList()

                    };
                    linxContext.PalletLogs.Add(log);
                    linxContext.SaveChanges();
                }
                );

            //retry<>
            return data.Match(
                Right: r =>
                {
                    var codes = r.Channels.ToDictionary(s => s.Index, s => s.Code);
                    var batteryCodes = Enumerable.Range(1, batteryTypeInfo.Count)
                        .Select(s => codes.GetValueOrDefault(s, ERROR))
                        .ToList(); // Todo: 如何简化
                    return new QRCodeResult()
                    {
                        ErrorCode = ErrorType.Successed,
                        Data = batteryCodes
                            .Select((s, i) => new { Index = i + 1, Code = s }) //Todo: 看看+1
                            .ToDictionary(s => s.Index, s => s.Code)
                    };
                },
                Left: e =>
                {
                    mediator.Send(new WarningBoxCommand($"检测失败 错误原因:{e}")).Wait();
                    return new QRCodeResult() { ErrorCode = ErrorType.CameraError };
                }
            );
        }
    }

    public enum ErrorType
    {
        Successed,
        CameraError,
        ERROR,
        SomeResultError,
    }

    public class QRCodeResult
    {
        public ErrorType ErrorCode { get; set; }
        public Dictionary<int, string> Data { get; set; }
    }
}
