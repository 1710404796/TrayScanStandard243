using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Camera.Fs.Common;
using LinxUniverse.CST;
using MugenCamera;
using TrayScanStandard.Models.CZPallet;
namespace TrayScanStandard.Utils
{    public static class DetectUtil
    {

        // 还需要一个光源的中间件

        /// <summary>
        /// 确保光源控制器数量与当前配置一致。
        /// 说明：兼容启动时未初始化/数量不一致的场景，避免拍照时不亮灯。
        /// </summary>
        private static void EnsureLightControllersReady()
        {
            var infos = MainStorage.Saves.LightInfos ?? [];
            var current = MainStorage.CST?.ToArray() ?? [];

            // 已经和配置一致时，不重复创建连接。
            if (infos.Length == current.Length)
            {
                return;
            }

            // 先关闭旧连接，避免串口占用。
            foreach (var cst in current)
            {
                try
                {
                    cst?.Close();
                }
                catch
                {
                    // 这里是兜底清理，不抛出异常影响拍照流程。
                }
            }

            var rebuilt = new List<LightCST>();
            foreach (var info in infos)
            {
                if (!Enum.TryParse<SerialPortType>(info.Com, true, out var com))
                {
                    continue;
                }

                var cst = new LightCST();
                // 沃德普明确固定 19200；康耐视沿用 DLL 串口初始化。
                var ok = info.Type == LightType.Wordop
                    ? cst.InitializeWordop((int)(com + 1), 19200)
                    : cst.Initialize((int)(com + 1));

                if (ok)
                {
                    rebuilt.Add(cst);
                }
            }

            MainStorage.CST = rebuilt;
        }

        /// <summary>
        /// 控制光源的开启和关闭
        /// </summary>
        /// <param name="turnOn">true为开启光源，false为关闭光源</param>
        private static void ControlLights(bool turnOn)
        {
            EnsureLightControllersReady();
            var lightInfos = MainStorage.Saves.LightInfos.Zip(MainStorage.CST);
            lightInfos.Iter(s =>
            {
                // 防御性保护：若某路光源初始化失败导致控制器为空，则跳过该路，避免拍照时空引用崩溃。
                if (s.Item2 == null || s.Item1.Values == null)
                {
                    return;
                }

                s.Item1.Values.Iter((i, v) =>
                {
                    s.Item2.SetLight(i + 1, turnOn ? v : 0);
                });
            });
        }

        /// <summary>
        /// 在开启光源的环境下执行同步操作
        /// </summary>
        public static Either<string, TResult> UseLight<TResult>
            (Func<Either<string, TResult>> func)
        {
            ControlLights(true);
            try
            {
                var result = func();
                return result;
            }
            finally
            {
                // 操作完成后务必关闭灯光。
                ControlLights(false);
            }
        }

        /// <summary>
        /// 在开启光源的环境下执行异步操作
        /// </summary>
        public static async Task<Either<string, TResult>> UseLight<TResult>
            (Func<Task<Either<string, TResult>>> func)
        {
            ControlLights(true);
            try
            {
                var result = await func();
                return result;
            }
            finally
            {
                // 操作完成后务必关闭灯光。
                ControlLights(false);
            }
        }

        public static Either<string, TResult> UseCamera<TResult>(
            MugenCamera.MugenCamera mugenCamera,
            Func<MugenCamera.MugenCamera, Either<string, TResult>> action)
        {
            mugenCamera.StopGrab();
            var camSession = mugenCamera.StartGrab();
            // 仅在 StartGrab 成功执行时执行该操作。
            var result = camSession.Bind(action);
            // 无论操作结果如何，只要StartGrab成功执行，就必须调用StopGrab。
            camSession.Bind(s => s.StopGrab());
            return result;
        }


        public static Either<string, Image2DResult> CaptureOne(this MugenCamera.MugenCamera mugenCamera)
        {
            // 定义具体操作：软件触发后捕获

            return
                UseCamera(mugenCamera, 
                    cam =>
                    cam.SoftwareTrigger() // 假设 SoftwareTrigger 返回的值为 string 或 MugenCamera。
                       .Bind(s => s.Capture(TimeSpan.FromSeconds(3)).Map(s => (s as Image2DResult)!)));
            // 使用辅助方法在会话中执行该操作
            //return ;
        }

        //public static Either<string, ImageData> CaptureOne(MugenCamera.MugenCamera mugenCamera)
        //{
        //    var cam = mugenCamera.StartGrab();
        //    var img = cam.Bind(MugenCameraExtensions.SoftwareTrigger)
        //        .Bind(s => s.Capture(TimeSpan.FromSeconds(3)));
        //    cam = cam.Bind(s => s.StopGrab());
        //    return cam.Bind(s => img);
        //}
        public static Either<string, IEnumerable<Image2DResult>> Capture(MugenCamera.MugenCamera[] mugenCameras)
        {
            return UseLight(
                () =>
                {
                    var results = mugenCameras
                        //.AsParallel()
                        //.AsOrdered()
                        .Select(cam => cam.CaptureOne())
                        .Traverse(s => s);
                    return results;
                });
            // 让所有相机拍个照
        }


        public static void CamCapture()
        {
            // 让所有相机拍个照

        }

        // 光源中间件‘

        public static void LightMiddle<T>(Func<T> f)
        {
            // 让所有光源中间件都亮
        }
    }
}
