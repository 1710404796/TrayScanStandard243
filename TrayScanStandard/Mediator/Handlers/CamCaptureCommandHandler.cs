using Camera.Fs.Common;
using HKCamera.Fs.NET.Controls;
using LinxUniverse.Utils;
using MediatR;
using MugenCamera;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrayScanStandard.Mediator.Commands;
using TrayScanStandard.Utils;

namespace TrayScanStandard.Mediator.Handlers
{
    // 此处用于处理相机拍照的命令
    public class CamCaptureCommandHandler : IRequestHandler<CamCaptureCommand, Either<string, IEnumerable<Image2DResult[]>>>
    {
        public Task<Either<string, IEnumerable<Image2DResult[]>>> Handle(CamCaptureCommand request, CancellationToken cancellationToken)
        {



            //var a = request.CaptureInfos.Select(s => 1);
            if (!MainStorage.Saves.CameraEnable)
            {
                return Either<string, IEnumerable<Image2DResult[]>>.Right
               ([..request.CaptureInfos
               .Select((s, i) => new Image2DResult[]
                       {
                           new Image2DResult(File.ReadAllBytes( @$"testImg\{i}.png"))
                           //new Image2DResult(File.ReadAllBytes( @$"C:\Users\admin\Pictures\20211110085254_736a4.jpeg"))
                       }
                 )
               ]).Apply(Task.FromResult);
            }

            return DetectUtil.UseLight(
                () => request.CaptureInfos
                    .Map(s => s.ToEither("相机未初始化"))
                    .Traverse(s => s)
                    .Bind(c =>
                    //.BindAsync(c =>
                        c
                        //.AsParallel()    //华睿相机并行拍照会有问题，暂时不使用
                        // .AsOrdered()


                        //.Select(s => Task.Run(() => ProcessCaptureInfo(s)))
                        //.Apply(Task.WhenAll)
                        //.Map(s => s.Traverse(q => q))

                        //.Select(s => new Func<Either<string, Image2DResult[]>>(() => ProcessCaptureInfo(s)))
                        //.Map(s => s.Invoke())
                        .Select(ProcessCaptureInfo)
                        .Traverse(s => s)


                        //.Select(ProcessCaptureInfo)
                        //.Traverse(s =>s)
                        )
                    //.TraverseParallel(s => s)
                    )
                .Apply(Task.FromResult)
                ;

            // 本地函数：处理单个CaptureInfo
            Either<string, Image2DResult[]> ProcessCaptureInfo(CaptureInfo s)
            {
                var aa = s.Exps.Map(e =>
                        s.Camera
                        .SetControl(new AcquisitionControl { ExposureTime = e })
                        .Bind(DetectUtil.CaptureOne)
                    )
                .Traverse(s => s)
                .Map(s => s.ToArray());
                return aa;
            }
        }
    }

}
