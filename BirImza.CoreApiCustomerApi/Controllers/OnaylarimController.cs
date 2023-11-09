using Microsoft.AspNetCore.Mvc;
using Flurl.Http;
using System.IO;
using System.Text.Json;

namespace BirImza.CoreApiCustomerApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class OnaylarimController : ControllerBase
    {
        /// <summary>
        /// Bu adresi test ortamı için https://apitest.onaylarim.com olarak değiştirmelisiniz
        /// </summary>
        private readonly string _onaylarimServiceUrl = "https://localhost:44337";
        //private readonly string _onaylarimServiceUrl = "https://apitest.onaylarim.com";
        //private readonly string _onaylarimServiceUrl = "http://api.beamimza.com";


        /// <summary>
        /// Size verilen API anahtarı ile değiştiriniz.
        /// </summary>
        private readonly string _apiKey = "278c0eb01c3f44e6ac0a64e43c478c0ab3e48a6fc4fe476987e52a3c8ced76b3";
        //private readonly string _apiKey = "865365135dff485b8ea0a2f515cbaaed08bf0f2444e646bdb31df6834fa5b126";
        //private readonly string _apiKey = "62e00e670b394e8b9c1b339d65383d8183e514e1fe0747598c5fd5c34c0de921";

        /// <summary>
        /// CADES ve PADES e-imza atma işlemi için ilk adımdır
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("CreateStateOnOnaylarimApi")]
        public async Task<CreateStateOnOnaylarimApiResult> CreateStateOnOnaylarimApi(CreateStateOnOnaylarimApiRequest request)
        {
            var result = new CreateStateOnOnaylarimApiResult();

            var operationId = Guid.NewGuid();

            if (request.SignatureType == "cades")
            {
                // İmzalanacak dosyayı kendi bilgisayarınızda bulunan bir dosya olarak ayarlayınız
                var fileData = System.IO.File.ReadAllBytes(@"C:\Temp\2023-04-14_Api_Development.log");

                try
                {
                    // Size verilen API key'i "X-API-KEY değeri olarak ayarlayınız
                    var signStepOneCoreResult = await $"{_onaylarimServiceUrl}/CoreApiCades/SignStepOneCadesCore"
                                    .WithHeader("X-API-KEY", _apiKey)
                                    .PostJsonAsync(
                                            new SignStepOneCadesCoreRequest()
                                            {
                                                CerBytes = request.Certificate,
                                                FileData = fileData,
                                                SignatureIndex = 0,
                                                OperationId = operationId,
                                                RequestId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 21),
                                                DisplayLanguage = "en"
                                            })
                                    .ReceiveJson<ApiResult<SignStepOneCadesCoreResult>>();

                    result.KeyID = signStepOneCoreResult.Result.KeyID;
                    result.KeySecret = signStepOneCoreResult.Result.KeySecret;
                    result.State = signStepOneCoreResult.Result.State;
                    result.OperationId = operationId;
                }
                catch (Exception ex)
                {

                }
            }

            else if (request.SignatureType == "pades")
            {
                // İmzalanacak dosyayı kendi bilgisayarınızda bulunan bir pdf olarak ayarlayınız
                //var fileData = System.IO.File.ReadAllBytes(@"C:\Users\ulucefe\Desktop\sample.pdf");
                //var fileData = System.IO.File.ReadAllBytes(@"C:\birimza\1\CoreAPI\372167CF-6143-4DB1-87CF-7AE7DC0FA1AB\Signed_20230821161316");
                var fileData = System.IO.File.ReadAllBytes(@"C:\Users\ulucefe\Downloads\100MB PDF File.pdf");
                var signatureWidgetBackground = System.IO.File.ReadAllBytes(@"C:\Users\ulucefe\Desktop\Signature01.jpg");

                try
                {

                    // Büyük dosyaların SignStepOnePadesCore metoduna json içerisinde gönderilmesi mümkün değildir.
                    // Bu nedenle, büyük dosyalar imzalanmak istendiğinde, önce SignStepOneUploadFile metodu ile dosya sunucuya yüklenir.
                    // Yükleme başarılı ise SignStepOnePadesCore metodu ile işleme devam edilir. Burada önemli olan, her iki metod için de aynı operationId değerini kullanmak gerekir
                    // Bu şekilde bir kullanım yapılması durumunda, SignStepOnePadesCoreRequest objesindeki FileData parametresi boş byte array olarak gönderilmelidir.
                    var uploadFileBeforeOperation = true;

                    if (uploadFileBeforeOperation)
                    {
                        var signStepOneUploadFileResult = await $"{_onaylarimServiceUrl}/CoreApiPades/SignStepOneUploadFile"
                                         .WithHeader("X-API-KEY", _apiKey)
                                         .WithHeader("operationid", operationId)
                                         .PostMultipartAsync(mp => mp
                                                 .AddFile("file", @"C:\Users\ulucefe\Downloads\100MB PDF File.pdf", null, 4096, "100MB PDF File.pdf")
                                         )
                                         .ReceiveJson<ApiResult<SignStepOneUploadFileResult>>();


                    }

                    // Size verilen API key'i "X-API-KEY değeri olarak ayarlayınız
                    var signStepOneCoreResult = await $"{_onaylarimServiceUrl}/CoreApiPades/SignStepOnePadesCore"
                                .WithHeader("X-API-KEY", _apiKey)
                                .PostJsonAsync(
                                        new SignStepOnePadesCoreRequest()
                                        {
                                            CerBytes = request.Certificate,
                                            FileData = uploadFileBeforeOperation ? new byte[] { } : fileData,
                                            SignatureIndex = 1,
                                            OperationId = operationId,
                                            RequestId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 21),
                                            DisplayLanguage = "en",
                                            VerificationInfo = new VerificationInfo()
                                            {
                                                Text = "Bu belge 5070 sayılı elektronik imza kanununa göre güvenli elektronik imza ile imzalanmıştır. Belgeye\r\nhttps://localhost:8082 adresinden 97275466-4A90128E46284E3181CF21020554BFEC452DBDE73",
                                                Width = 0.8f,
                                                Height = 0.1f,
                                                Left = 0.1f,
                                                Bottom = 0.03f,
                                                TransformOrigin = "left bottom"
                                            },
                                            QrCodeInfo = new QrCodeInfo()
                                            {
                                                Text = "google.com",
                                                Width = 0.1f,
                                                Right = 0.03f,
                                                Top = 0.02f,
                                                TransformOrigin = "right top"
                                            },
                                            SignatureWidgetInfo = new SignatureWidgetInfo()
                                            {
                                                Width = 100f,
                                                Height = 50f,
                                                Left = 0.5f,
                                                Top = 0.03f,
                                                TransformOrigin = "left top",
                                                ImageBytes = signatureWidgetBackground,
                                                PagesToPlaceOn = new int[] { 0 },
                                                Lines = new List<LineInfo>()
                                                {
                                                        new LineInfo()
                                                        {
                                                             BottomMargin=4,
                                                             ColorHtml="#000000",
                                                             FontName="Arial",
                                                             FontSize=10,
                                                             FontStyle = "Bold",
                                                             LeftMargin=4,
                                                             RightMargin=4,
                                                             Text="Uluç Efe Öztürk",
                                                             TopMargin=4
                                                        },
                                                        new LineInfo()
                                                        {
                                                             BottomMargin=4,
                                                             ColorHtml="#FF0000",
                                                             FontName="Arial",
                                                             FontSize=10,
                                                             FontStyle = "Regular",
                                                             LeftMargin=4,
                                                             RightMargin=4,
                                                             Text="2022-11-11",
                                                             TopMargin=4
                                                        }
                                                }
                                            }
                                        })
                                .ReceiveJson<ApiResult<SignStepOnePadesCoreResult>>();


                    if (string.IsNullOrWhiteSpace(signStepOneCoreResult.Error))
                    {
                        result.KeyID = signStepOneCoreResult.Result.KeyID;
                        result.KeySecret = signStepOneCoreResult.Result.KeySecret;
                        result.State = signStepOneCoreResult.Result.State;
                        result.OperationId = operationId;
                    }
                    else
                    {
                        result.Error = signStepOneCoreResult.Error;
                    }

                }
                catch (Exception ex)
                {
                }
            }
            return result;

        }

        /// <summary>
        /// CADES ve PADES e-imza atma işlemi için son adımdır
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("FinishSign")]
        public async Task<FinishSignResult> FinishSign(FinishSignRequest request)
        {
            var result = new FinishSignResult();

            if (request.SignatureType == "cades")
            {
                try
                {
                    // Size verilen API key'i "X-API-KEY değeri olarak ayarlayınız
                    var signStepOneCoreResult = await $"{_onaylarimServiceUrl}/CoreApiCades/signStepThreeCadesCore"
                                    .WithHeader("X-API-KEY", _apiKey)
                                    .PostJsonAsync(
                                            new SignStepThreeCadesCoreRequest()
                                            {
                                                SignedData = request.SignedData,
                                                KeyId = request.KeyId,
                                                KeySecret = request.KeySecret,
                                                OperationId = request.OperationId,
                                                RequestId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 21),
                                                DisplayLanguage = "en"
                                            })
                                    .ReceiveJson<ApiResult<SignStepThreeCadesCoreResult>>();

                    result.IsSuccess = signStepOneCoreResult.Result.IsSuccess;

                }
                catch (Exception ex)
                {
                }
            }
            else if (request.SignatureType == "pades")
            {
                try
                {
                    // Size verilen API key'i "X-API-KEY değeri olarak ayarlayınız
                    var signStepOneCoreResult = await $"{_onaylarimServiceUrl}/CoreApiPades/signStepThreePadesCore"
                                    .WithHeader("X-API-KEY", _apiKey)
                                    .PostJsonAsync(
                                            new SignStepThreePadesCoreRequest
                                            {
                                                SignedData = request.SignedData,
                                                KeyId = request.KeyId,
                                                KeySecret = request.KeySecret,
                                                OperationId = request.OperationId,
                                                RequestId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 21),
                                                DisplayLanguage = "en"
                                            })
                                    .ReceiveJson<ApiResult<SignStepThreePadesCoreResult>>();

                    result.IsSuccess = signStepOneCoreResult.Result.IsSuccess;

                }
                catch (Exception ex)
                {
                }
            }

            return result;
        }

        /// <summary>
        /// Mobil imza atma işlemi için kullanılan metoddur
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("MobileSign")]
        public async Task<MobilSignResult> MobileSign(MobileSignRequest request)
        {
            var result = new MobilSignResult();

            if (request.SignatureType == "cades")
            {

            }
            else if (request.SignatureType == "pades")
            {
                // İmzalanacak dosyayı kendi bilgisayarınızda bulunan bir pdf olarak ayarlayınız
                var fileData = System.IO.File.ReadAllBytes(@"C:\Users\ulucefe\Desktop\sample.pdf");

                try
                {
                    // Size verilen API key'i "X-API-KEY değeri olarak ayarlayınız
                    var signStepOneCoreResult = await $"{_onaylarimServiceUrl}/CoreApiPadesMobile/SignStepOnePadesMobileCore"
                                    .WithHeader("X-API-KEY", _apiKey)
                                    .PostJsonAsync(
                                            new SignStepOnePadesMobileCoreRequest()
                                            {
                                                FileData = fileData,
                                                SignatureIndex = 0,
                                                OperationId = request.OperationId,
                                                RequestId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 21),
                                                DisplayLanguage = "en",
                                                VerificationInfo = new VerificationInfo()
                                                {
                                                    Text = "Bu belge 5070 sayılı elektronik imza kanununa göre güvenli elektronik imza ile imzalanmıştır. Belgeye\r\nhttps://localhost:8082 adresinden 97275466-4A90128E46284E3181CF21020554BFEC452DBDE73",
                                                    Width = 0.8f,
                                                    Height = 0.1f,
                                                    Left = 0.1f,
                                                    Bottom = 0.03f,
                                                    TransformOrigin = "left bottom"
                                                },
                                                QrCodeInfo = new QrCodeInfo()
                                                {
                                                    Text = "google.com",
                                                    Width = 0.1f,
                                                    Right = 0.03f,
                                                    Top = 0.02f,
                                                    TransformOrigin = "right top"
                                                },
                                                PhoneNumber = request.PhoneNumber,
                                                Operator = request.Operator,
                                                UserPrompt = "CoreAPI ile belge imzalayacaksınız."
                                            })
                                    .ReceiveJson<ApiResult<SignStepOneCoreInternalForPadesMobileResult>>();

                    if (string.IsNullOrWhiteSpace(signStepOneCoreResult.Error) == false)
                    {
                        result.Error = signStepOneCoreResult.Error;
                    }
                    else
                    {
                        result.IsSuccess = signStepOneCoreResult.Result.IsSuccess;
                    }
                }
                catch (Exception ex)
                {
                    result.Error = ex.Message;
                }
            }
            return result;

        }

        [HttpPost("GetFingerPrint")]
        public async Task<GetFingerPrintResult> GetFingerPrint(GetFingerPrintRequest request)
        {
            var result = new GetFingerPrintResult();
            try
            {
                // Size verilen API key'i "X-API-KEY değeri olarak ayarlayınız
                var getFingerPrintCoreResult = await $"{_onaylarimServiceUrl}/CoreApiPadesMobile/GetFingerPrintCore"
                                .WithHeader("X-API-KEY", _apiKey)
                                .PostJsonAsync(
                                        new GetFingerPrintCoreRequest()
                                        {
                                            OperationId = request.OperationId,
                                            RequestId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 21),
                                            DisplayLanguage = "en",
                                        })
                                .ReceiveJson<ApiResult<GetFingerPrintCoreResult>>();

                result.FingerPrint = getFingerPrintCoreResult.Result.FingerPrint;
            }
            catch (Exception ex)
            {
            }
            return result;

        }

        /// <summary>
        /// İmzalanmış dosyayı indirmek için kullanılır
        /// </summary>
        /// <param name="operationId"></param>
        /// <returns></returns>
        [HttpGet("DownloadSignedFileFromOnaylarimApi")]
        public async Task<IActionResult> DownloadSignedFileFromOnaylarimApi(Guid operationId)
        {

            try
            {
                // Size verilen API key'i "X-API-KEY değeri olarak ayarlayınız
                var downloadSignedFileCoreResult = await $"{_onaylarimServiceUrl}/CoreApiDownload/downloadSignedFileCore"
                                        .WithHeader("X-API-KEY", _apiKey)
                                        .PostJsonAsync(
                                            new DownloadSignedFileCoreRequest()
                                            {
                                                OperationId = operationId,
                                                RequestId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 21),
                                                DisplayLanguage = "en"
                                            })
                                        .ReceiveJson<ApiResult<DownloadSignedFileCoreResult>>();

                return File(downloadSignedFileCoreResult.Result.FileData, "application/octet-stream", downloadSignedFileCoreResult.Result.FileName + ".imz");

            }
            catch (Exception ex)
            {
            }
            return BadRequest("Hata");
        }


        /// <summary>
        /// Microsoft Office ve resim dosyalarını pdf'e dönüştürür
        /// </summary>
        /// <returns></returns>
        [HttpGet("ConvertToPdf")]
        public async Task<IActionResult> ConvertToPdf()
        {
            // Dosyayı kendi bilgisayarınızda bulunan bir dosya olarak ayarlayınız
            var fileData = System.IO.File.ReadAllBytes(@"C:\Users\ulucefe\Downloads\yeni proje.docx");

            var operationId = Guid.NewGuid();
            try
            {
                // Size verilen API key'i "X-API-KEY değeri olarak ayarlayınız
                var signStepOneCoreResult = await $"{_onaylarimServiceUrl}/CoreApiPdf/ConvertToPdfCore"
                                .WithHeader("X-API-KEY", _apiKey)
                                .PostJsonAsync(
                                        new ConvertToPdfCoreRequest()
                                        {
                                            FileData = fileData,
                                            FileName = "yeni proje.docx",
                                            OperationId = operationId,
                                            RequestId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 21),
                                            DisplayLanguage = "en",
                                        })
                                .ReceiveJson<ApiResult<ConvertToPdfCoreResult>>();

                return File(signStepOneCoreResult.Result.FileData, "application/pdf", "pdf000.pdf");

            }
            catch (Exception ex)
            {
            }
            return BadRequest("Hata");

        }

        /// <summary>
        /// PDF dosyasının üzerine doğrulama cümlesi ve karekod basar
        /// </summary>
        /// <returns></returns>
        [HttpGet("AddLayers")]
        public async Task<IActionResult> AddLayers()
        {

            // Dosyayı kendi bilgisayarınızda bulunan bir dosya olarak ayarlayınız
            var fileData = System.IO.File.ReadAllBytes(@"C:\Users\ulucefe\Desktop\sample.pdf");

            try
            {
                // Size verilen API key'i "X-API-KEY değeri olarak ayarlayınız
                var signStepOneCoreResult = await $"{_onaylarimServiceUrl}/CoreApiPdf/AddLayersCore"
                                .WithHeader("X-API-KEY", _apiKey)
                                .PostJsonAsync(
                                        new AddLayersCoreRequest()
                                        {
                                            FileData = fileData,
                                            FileName = "sample.pdf",
                                            OperationId = Guid.NewGuid(),
                                            RequestId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 21),
                                            DisplayLanguage = "en",
                                            VerificationInfo = new VerificationInfo()
                                            {
                                                Text = "Bu belge 5070 sayılı elektronik imza kanununa göre güvenli elektronik imza ile imzalanmıştır. Belgeye\r\nhttps://localhost:8082 adresinden 97275466-4A90128E46284E3181CF21020554BFEC452DBDE73",
                                                Width = 0.8f,
                                                Height = 0.1f,
                                                Left = 0.1f,
                                                Bottom = 0.03f,
                                                TransformOrigin = "left bottom"
                                            },
                                            QrCodeInfo = new QrCodeInfo()
                                            {
                                                Text = "google.com",
                                                Width = 0.1f,
                                                Right = 0.03f,
                                                Top = 0.02f,
                                                TransformOrigin = "right top"
                                            }
                                        })
                                .ReceiveJson<ApiResult<AddLayersCoreResult>>();

                return File(signStepOneCoreResult.Result.FileData, "application/pdf", "pdf000.pdf");

            }
            catch (Exception ex)
            {
            }
            return BadRequest("Hata");

        }

        [HttpGet("VerifySignaturesOnOnaylarimApi")]
        public async Task<VerifySignaturesCoreResult> VerifySignaturesOnOnaylarimApi()
        {
            var result = new VerifySignaturesCoreResult();

            var operationId = Guid.NewGuid();

            try
            {

                var verifySignaturesCoreResult = await $"{_onaylarimServiceUrl}/CoreApiPades/VerifySignaturesCore"
                                     .WithHeader("X-API-KEY", _apiKey)
                                     .WithHeader("operationid", operationId)
                                     .PostMultipartAsync(mp => mp
                                             .AddFile("file", @"C:\Users\ulucefe\Downloads\cok imzali.pdf", null, 4096, "cok imzali.pdf")
                                     )
                                     .ReceiveJson<ApiResult<VerifySignaturesCoreResult>>();

                return verifySignaturesCoreResult.Result;


            }
            catch (Exception ex)
            {
            }

            return result;

        }

    }

    public class AddLayersCoreResult
    {
        /// <summary>
        /// Üzerine layer eklenmiş dosya verisidir
        /// </summary>
        public byte[] FileData { get; set; }
    }

    public class AddLayersCoreRequest : BaseRequest
    {
        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }
        /// <summary>
        /// Üzerine layer eklenecek dosya verisidir
        /// </summary>
        public byte[] FileData { get; set; }
        /// <summary>
        /// Üzerine layer eklenecek dosya adıdır
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// İmzalanacak doküman üzerinde eklenecek cümleyle ilgili bilgileri içerir
        /// </summary>
        public VerificationInfo VerificationInfo { get; set; }
        /// <summary>
        /// İmzalanacak doküman üzerinde eklenecek QRCode ilgili bilgileri içerir
        /// </summary>
        public QrCodeInfo QrCodeInfo { get; set; }
    }

    /// <summary>
    /// Tüm API metodları bu wrapper class'ı döner
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResult<T>
    {
        /// <summary>
        /// API metodu sonucunda dönen değerdir
        /// </summary>
        public T Result { get; set; }
        /// <summary>
        /// Varsa hataya ilişkin mesaj ve açıklayıcı bilgi dönülür
        /// </summary>
        public string Error { get; set; }

    }

    public class BaseRequest
    {
        /// <summary>
        /// Her bir request'i tekil olarak ayırt eden, request trail için kullanılan değerdir. 21 karakter uzunluğunda random bir değer atanmalıdır
        /// </summary>
        public string RequestId { get; set; }
        /// <summary>
        /// Dönen hata mesajlarının hangi dilde dönmesi gerektiğini belirtir, olası değerler en ve tr'dir.
        /// </summary>
        public string DisplayLanguage { get; set; }
    }

    public class ConvertToPdfCoreResult
    {
        /// <summary>
        /// Dönüştürülmüş dosya verisidir
        /// </summary>
        public byte[] FileData { get; set; }
    }

    public class ConvertToPdfCoreRequest : BaseRequest
    {
        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }
        /// <summary>
        /// Döüştürülecek dosya verisidir
        /// </summary>
        public byte[] FileData { get; set; }
        /// <summary>
        /// Döüştürülecek dosya adıdır
        /// </summary>
        public string FileName { get; set; }
    }

    public class CreateStateOnOnaylarimApiRequest
    {
        /// <summary>
        /// Son kullanıcı bilgisayarında bulunan e-İmza Aracı vasıtasıyla alınan, e-imza atarken kullanılacak sertifikadır
        /// </summary>
        public string Certificate { get; set; }
        /// <summary>
        /// atılacak e-imza türüdür, değerler pades ve cades olabilir
        /// </summary>
        public string SignatureType { get; set; }
    }

    public class CreateStateOnOnaylarimApiResult
    {
        /// <summary>
        /// e-İmza aracına iletilecek, e-imza state'idir.
        /// </summary>
        public string State { get; set; }
        /// <summary>
        /// Mevcut e-imza işlemine ait ID değeridir. e-İmza aracına iletilir.
        /// </summary>
        public string KeyID { get; set; }
        /// <summary>
        /// Mevcut e-imza işlemine ait KeySecret değeridir. e-İmza aracına iletilir.
        /// </summary>
        public string KeySecret { get; set; }
        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }
        public string Error { get; set; }
    }

    public class DownloadSignedFileCoreResult
    {
        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }
        /// <summary>
        /// Dosya verisidir
        /// </summary>
        public byte[] FileData { get; set; }
        /// <summary>
        /// Dosyanın adıdır
        /// </summary>
        public string FileName { get; set; }
    }

    public class DownloadSignedFileCoreRequest : BaseRequest
    {

        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }
    }

    public class GetFingerPrintCoreRequest : BaseRequest
    {
        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }
    }

    public class GetFingerPrintCoreResult
    {
        public string FingerPrint { get; set; }
    }
    public class GetFingerPrintRequest
    {
        public Guid OperationId { get; set; }
    }

    public class GetFingerPrintResult
    {
        public string FingerPrint { get; set; }
    }

    public class FinishSignRequest
    {

        /// <summary>
        /// e-İmza aracı tarafından imzalanmış veri
        /// </summary>
        public string SignedData { get; set; }
        /// <summary>
        /// Mevcut e-imza işlemine ait ID değeridir. e-İmza aracına iletilir.
        /// </summary>
        public string KeyId { get; set; }
        /// <summary>
        /// Mevcut e-imza işlemine ait KeySecret değeridir. e-İmza aracına iletilir.
        /// </summary>
        public string KeySecret { get; set; }
        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }
        /// <summary>
        /// atılacak e-imza türüdür, değerler pades ve cades olabilir
        /// </summary>
        public string SignatureType { get; set; }
    }

    public class FinishSignResult
    {
        /// <summary>
        /// İşlemin başarıyla tamamlanıp tamamlanmadığını gösterir
        /// </summary>
        public bool IsSuccess { get; set; }
    }

    public class MobilSignResult
    {
        /// <summary>
        /// İşlemin başarıyla tamamlanıp tamamlanmadığını gösterir
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// Hata var ise detay bilgisi döner.
        /// </summary>
        public string Error { get; set; }
    }

    public class MobileSignRequest
    {
        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }
        /// <summary>
        /// atılacak e-imza türüdür, değerler pades ve cades olabilir
        /// </summary>
        public string SignatureType { get; set; }
        /// <summary>
        /// İmza atarken kullanılacak mobil imzaya ait telefon numarasıdır. Örnek: 5446786666
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// İmza atarken kullanılacak mobil imzaya ait telefon numarasının bağlı olduğu operatördür. Örnek: TURKCELL, VODAFONE, AVEA
        /// </summary>
        public string Operator { get; set; }
    }

    public class SignStepOneCadesCoreRequest : BaseRequest
    {
        /// <summary>
        /// Son kullanıcı bilgisayarında bulunana e-İmza Aracı vasıtasıyla alınan, e-imza atarken kullanılacak sertifikadır
        /// </summary>
        public string CerBytes { get; set; }
        /// <summary>
        /// İmzalanacak dosyadır
        /// </summary>
        public byte[] FileData { get; set; }
        /// <summary>
        /// Dosya üzerinde kaçıncı imza olduğu bilgisidir. Dosya üzerinde hiç imza yok ise 0 değeri atanır.
        /// </summary>
        public int SignatureIndex { get; set; }
        /// <summary>
        /// Son kullanıcının geolocation bilgisidir. API bu alanı şimdilik kullanmamaktadır. Bu nedenle null olarak atanabilir.
        /// </summary>
        public SignStepOneRequestCoordinates Coordinates { get; set; }
        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }

    }

    public class SignStepOneCadesCoreResult
    {
        /// <summary>
        /// e-İmza aracına iletilecek, e-imza state'idir.
        /// </summary>
        public string State { get; set; }
        /// <summary>
        /// Mevcut e-imza işlemine ait ID değeridir. e-İmza aracına iletilir.
        /// </summary>
        public string KeyID { get; set; }
        /// <summary>
        /// Mevcut e-imza işlemine ait KeySecret değeridir. e-İmza aracına iletilir.
        /// </summary>
        public string KeySecret { get; set; }
    }

    public class SignStepOneCoreInternalForPadesMobileResult
    {
        /// <summary>
        /// İşlemin başarıyla tamamlanıp tamamlanmadığını gösterir
        /// </summary>
        public bool IsSuccess { get; set; }
    }

    public class SignStepOnePadesMobileCoreRequest : BaseRequest
    {
        /// <summary>
        /// İmzalanacak dosyadır
        /// </summary>
        public byte[] FileData { get; set; }
        /// <summary>
        /// Dosya üzerinde kaçıncı imza olduğu bilgisidir. Dosya üzerinde hiç imza yok ise 0 değeri atanır.
        /// </summary>
        public int SignatureIndex { get; set; }
        /// <summary>
        /// Son kullanıcının geolocation bilgisidir. API bu alanı şimdilik kullanmamaktadır. Bu nedenle null olarak atanabilir.
        /// </summary>
        public SignStepOneRequestCoordinates Coordinates { get; set; }
        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }
        /// <summary>
        /// İmzalanacak doküman üzerinde eklenecek cümleyle ilgili bilgileri içerir
        /// </summary>
        public VerificationInfo VerificationInfo { get; set; }
        /// <summary>
        /// İmzalanacak doküman üzerinde eklenecek QRCode ilgili bilgileri içerir
        /// </summary>
        public QrCodeInfo QrCodeInfo { get; set; }
        /// <summary>
        /// İmza atarken kullanılacak mobil imzaya ait telefon numarasıdır. Örnek: 5446786666
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// İmza atarken kullanılacak mobil imzaya ait telefon numarasının bağlı olduğu operatördür. Örnek: TURKCELL, VODAFONE, AVEA
        /// </summary>
        public string Operator { get; set; }
        /// <summary>
        /// İmza atarken kullanıcıya gösterilecek mesaj
        /// </summary>
        public string UserPrompt { get; set; }
    }

    public class SignStepOnePadesCoreRequest : BaseRequest
    {

        /// <summary>
        /// Son kullanıcı bilgisayarında bulunana e-İmza Aracı vasıtasıyla alınan, e-imza atarken kullanılacak sertifikadır
        /// </summary>
        public string CerBytes { get; set; }
        /// <summary>
        /// İmzalanacak dosyadır
        /// </summary>
        public byte[] FileData { get; set; }
        /// <summary>
        /// Dosya üzerinde kaçıncı imza olduğu bilgisidir. Dosya üzerinde hiç imza yok ise 0 değeri atanır.
        /// </summary>
        public int SignatureIndex { get; set; }
        /// <summary>
        /// Son kullanıcının geolocation bilgisidir. API bu alanı şimdilik kullanmamaktadır. Bu nedenle null olarak atanabilir.
        /// </summary>
        public SignStepOneRequestCoordinates Coordinates { get; set; }
        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }
        /// <summary>
        /// İmzalanacak doküman üzerinde eklenecek cümleyle ilgili bilgileri içerir
        /// </summary>
        public VerificationInfo VerificationInfo { get; set; }
        /// <summary>
        /// İmzalanacak doküman üzerinde eklenecek QRCode ilgili bilgileri içerir
        /// </summary>
        public QrCodeInfo QrCodeInfo { get; set; }
        /// <summary>
        /// Sayfa üzerine eklenecek imza görseli bilgisidir
        /// </summary>
        public SignatureWidgetInfo SignatureWidgetInfo { get; set; }
    }




    public class SignStepOnePadesCoreResult
    {
        /// <summary>
        /// e-İmza aracına iletilecek, e-imza state'idir.
        /// </summary>
        public string State { get; set; }
        /// <summary>
        /// Mevcut e-imza işlemine ait ID değeridir. e-İmza aracına iletilir.
        /// </summary>
        public string KeyID { get; set; }
        /// <summary>
        /// Mevcut e-imza işlemine ait KeySecret değeridir. e-İmza aracına iletilir.
        /// </summary>
        public string KeySecret { get; set; }
    }

    public class SignStepOneRequestCoordinates
    {
        /// <summary>
        /// The accuracy of position
        /// </summary>
        public double? Accuracy { get; set; }
        /// <summary>
        /// The altitude in meters above the mean sea level
        /// </summary>
        public double? Altitude { get; set; }
        /// <summary>
        /// The altitude accuracy of position
        /// </summary>
        public double? AltitudeAccuracy { get; set; }
        /// <summary>
        /// The heading as degrees clockwise from North
        /// </summary>
        public double? Heading { get; set; }
        /// <summary>
        /// The latitude as a decimal number
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// The longitude as a decimal number
        /// </summary>
        public double Longitude { get; set; }
        /// <summary>
        /// The speed in meters per second
        /// </summary>
        public double? Speed { get; set; }
    }

    public class SignStepThreeCadesCoreResult
    {
        /// <summary>
        /// İşlemin başarıyla tamamlanıp tamamlanmadığını gösterir
        /// </summary>
        public bool IsSuccess { get; set; }
    }

    public class SignStepThreeCadesCoreRequest : BaseRequest
    {
        /// <summary>
        /// e-İmza aracı tarafından imzalanmış veri
        /// </summary>
        public string SignedData { get; set; }
        /// <summary>
        /// Mevcut e-imza işlemine ait ID değeridir. e-İmza aracına iletilir.
        /// </summary>
        public string KeyId { get; set; }
        /// <summary>
        /// Mevcut e-imza işlemine ait KeySecret değeridir. e-İmza aracına iletilir.
        /// </summary>
        public string KeySecret { get; set; }
        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }
    }

    public class SignStepThreePadesCoreResult
    {
        /// <summary>
        /// İşlemin başarıyla tamamlanıp tamamlanmadığını gösterir
        /// </summary>
        public bool IsSuccess { get; set; }
    }

    public class SignStepThreePadesCoreRequest : BaseRequest
    {
        /// <summary>
        /// e-İmza aracı tarafından imzalanmış veri
        /// </summary>
        public string SignedData { get; set; }
        /// <summary>
        /// Mevcut e-imza işlemine ait ID değeridir. e-İmza aracına iletilir.
        /// </summary>
        public string KeyId { get; set; }
        /// <summary>
        /// Mevcut e-imza işlemine ait KeySecret değeridir. e-İmza aracına iletilir.
        /// </summary>
        public string KeySecret { get; set; }
        /// <summary>
        /// Her bir istek için tekil bir GUID değeri verilmelidir. Bu değer aynı e-imza işlemi ile ilgili olarak daha sonraki metodlarda kullanılır.
        /// </summary>
        public Guid OperationId { get; set; }
    }

    public class QrCodeInfo
    {
        /// <summary>
        /// QR kod içinde yazacak URL bilgisidir
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Karekodun genişliğinin sayfa genişliğine oranıdır. Sayfa genişliği 1000 olan bir sayfa için width değer 0.8 verilirse, karekod genişliği 800 olur. Karekodun genişliği ve yüksekliği eşittir
        /// </summary>
        public float Width { get; set; }
        /// <summary>
        /// Karekodun sayfanın solundan olan uzaklığıdır. Sayfa genişliği 1000 olan bir sayfa için left değer 0.1 verilirse, karekod sayfanın solundan uzaklığı 100 olur. Left ve Right değerleri aynı anda kullanılmamalı, sadece biri kullanılmalıdır.
        /// </summary>
        public float? Left { get; set; }
        /// <summary>
        /// Karekodun sayfanın sağından olan uzaklığıdır. Sayfa genişliği 1000 olan bir sayfa için right değer 0.1 verilirse, karekod sayfanın sağından uzaklığı 100 olur. Left ve Right değerleri aynı anda kullanılmamalı, sadece biri kullanılmalıdır.
        /// </summary>
        public float? Right { get; set; }
        /// <summary>
        /// Karekodun sayfanın üstünden olan uzaklığıdır. Sayfa yüksekliği 1000 olan bir sayfa için top değer 0.1 verilirse, karekod sayfanın üstünden uzaklığı 100 olur. Top ve Bottom değerleri aynı anda kullanılmamalı, sadece biri kullanılmalıdır.
        /// </summary>
        public float? Top { get; set; }
        /// <summary>
        /// Karekodun sayfanın altından olan uzaklığıdır. Sayfa yüksekliği 1000 olan bir sayfa için bottom değer 0.1 verilirse, karekod sayfanın altından uzaklığı 100 olur. Top ve Bottom değerleri aynı anda kullanılmamalı, sadece biri kullanılmalıdır.
        /// </summary>
        public float? Bottom { get; set; }
        /// <summary>
        /// Karekodun lokasyonu için hangi parametrelerin kullanılması gerektiği bilgisidir. Örnekler: "left top", "right top"
        /// </summary>
        public string TransformOrigin { get; set; }
    }

    public class VerificationInfo
    {
        /// <summary>
        /// Doğrulama cümlesidir. Yeni satır için \r\n değeri girilebilir. Örnek: Satır 1\r\nSatır2
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// İmzalama cümlesi kutusunun, sayfa genişliğine oranıdır. Sayfa genişliği 1000 olan bir sayfa için width değer 0.8 verilirse, doğrulama cümlesi genişliği 800 olur.
        /// </summary>
        public float Width { get; set; }
        /// <summary>
        /// İmzalama cümlesi kutusunun, sayfa yüksekliğine oranıdır. Sayfa yüksekliği 1000 olan bir sayfa için height değer 0.1 verilirse, doğrulama cümlesi yüksekliği 100 olur.
        /// </summary>
        public float Height { get; set; }
        /// <summary>
        /// İmzalama cümlesi kutusunun sayfanın solundan olan uzaklığıdır. Sayfa genişliği 1000 olan bir sayfa için left değer 0.1 verilirse, doğrulama cümlesinin sayfanın solundan uzaklığı 100 olur. Left ve Right değerleri aynı anda kullanılmamalı, sadece biri kullanılmalıdır.
        /// </summary>
        public float? Left { get; set; }
        /// <summary>
        /// İmzalama cümlesi kutusunun sayfanın sağından olan uzaklığıdır. Sayfa genişliği 1000 olan bir sayfa için right değer 0.1 verilirse, doğrulama cümlesinin sayfanın sağından uzaklığı 100 olur. Left ve Right değerleri aynı anda kullanılmamalı, sadece biri kullanılmalıdır.
        /// </summary>
        public float? Right { get; set; }
        /// <summary>
        /// İmzalama cümlesi kutusunun sayfanın üstünden olan uzaklığıdır. Sayfa yüksekliği 1000 olan bir sayfa için top değer 0.1 verilirse, doğrulama cümlesinin sayfanın üstünden uzaklığı 100 olur. Top ve Bottom değerleri aynı anda kullanılmamalı, sadece biri kullanılmalıdır.
        /// </summary>
        public float? Top { get; set; }
        /// <summary>
        /// İmzalama cümlesi kutusunun sayfanın altından olan uzaklığıdır. Sayfa yüksekliği 1000 olan bir sayfa için bottom değer 0.1 verilirse, doğrulama cümlesinin sayfanın altından uzaklığı 100 olur. Top ve Bottom değerleri aynı anda kullanılmamalı, sadece biri kullanılmalıdır.
        /// </summary>
        public float? Bottom { get; set; }
        /// <summary>
        /// İmzalama cümlesi kutusunun lokasyonu için hangi parametrelerin kullanılması gerektiği bilgisidir. Örnekler: "left top", "right top"
        /// </summary>
        public string TransformOrigin { get; set; }
    }

    public class SignatureWidgetInfo
    {
        /// <summary>
        /// İmzanın pixel olarak genişliğidir
        /// </summary>
        public float Width { get; set; }
        /// <summary>
        /// İmzanın pixel olarak yüksekliğidir
        /// </summary>
        public float Height { get; set; }
        /// <summary>
        /// İmzanın sayfanın solundan olan uzaklığıdır. Sayfa genişliği 1000 olan bir sayfa için left değer 0.1 verilirse, imza sayfanın solundan uzaklığı 100 olur. Left ve Right değerleri aynı anda kullanılmamalı, sadece biri kullanılmalıdır.
        /// </summary>
        public float? Left { get; set; }
        /// <summary>
        /// İmzanın sayfanın sağından olan uzaklığıdır. Sayfa genişliği 1000 olan bir sayfa için right değer 0.1 verilirse, imza sayfanın sağından uzaklığı 100 olur. Left ve Right değerleri aynı anda kullanılmamalı, sadece biri kullanılmalıdır.
        /// </summary>
        public float? Right { get; set; }
        /// <summary>
        /// İmzanın sayfanın üstünden olan uzaklığıdır. Sayfa yüksekliği 1000 olan bir sayfa için top değer 0.1 verilirse, imza sayfanın üstünden uzaklığı 100 olur. Top ve Bottom değerleri aynı anda kullanılmamalı, sadece biri kullanılmalıdır.
        /// </summary>
        public float? Top { get; set; }
        /// <summary>
        /// İmzanın sayfanın altından olan uzaklığıdır. Sayfa yüksekliği 1000 olan bir sayfa için bottom değer 0.1 verilirse, imza sayfanın altından uzaklığı 100 olur. Top ve Bottom değerleri aynı anda kullanılmamalı, sadece biri kullanılmalıdır.
        /// </summary>
        public float? Bottom { get; set; }
        // <summary>
        /// İmzanın lokasyonu için hangi parametrelerin kullanılması gerektiği bilgisidir. Örnekler: "left top", "right top"
        /// </summary>
        public string TransformOrigin { get; set; }
        /// <summary>
        /// İmza görselinde arka plan görseli olarak kullanılacak imajın datasıdır. İmaj jpg olmalıdır
        /// </summary>
        public byte[] ImageBytes { get; set; }
        /// <summary>
        /// İmza görselinin hangi sayfalara yerleştirileceği bilgisidir, 0 dan başlar.
        /// </summary>
        public int[] PagesToPlaceOn { get; set; }
        /// <summary>
        /// İmza görseli içerisinde yazılacak ifadelerdir.
        /// </summary>
        public List<LineInfo> Lines { get; set; }
    }

    public class LineInfo
    {
        /// <summary>
        /// Satır içerisinde yazacak ifadedir
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Satırın sol marjinidir.
        /// </summary>
        public int LeftMargin { get; set; }
        /// <summary>
        /// Satırın tepe marjinidir.
        /// </summary>
        public int TopMargin { get; set; }
        /// <summary>
        /// Satırın alt marjinidir.
        /// </summary>
        public int BottomMargin { get; set; }
        /// <summary>
        /// Satırın sağ marjinidir.
        /// </summary>
        public int RightMargin { get; set; }
        /// <summary>
        /// Satırın hangi font ile yazılacağını belirler. Arial, Tahoma gibi kullanınız.
        /// </summary>
        public string FontName { get; set; }
        /// <summary>
        /// Satırın hangi font büyüklüğü ile yazılacağını belirler.
        /// </summary>
        public float FontSize { get; set; }
        /// <summary>
        /// Satırın hangi font tipi ile yazılacağını belirler. Regular, Bold, Italic, Underline, Strikeout
        /// </summary>
        public string FontStyle { get; set; }
        /// <summary>
        /// Satırın hangi renkle yazılacağını belirleri. #FF00FF gibi kullanınız.
        /// </summary>
        public string ColorHtml { get; set; }

    }

    public class SignStepOneUploadFileResult
    {
        public bool IsSuccess { get; set; }
        public Guid OperationId { get; set; }
    }

    public class VerifySignaturesCoreResult
    {
        public bool CaptchaError { get; set; }
        public bool AllSignaturesValid { get; set; }
        public List<VerifyDocumentResultSignature> Signatures { get; set; }
        public List<VerifyDocumentResultTimestamp> Timestamps { get; set; }
        public string FileName { get; set; }
        public string SignatureType { get; set; }
    }

    public class VerifyDocumentResultSignature
    {
        public string ChainValidationResult { get; set; }
        public DateTime ClaimedSigningTime { get; set; }
        public string HashAlgorithm { get; set; }
        public string Profile { get; set; }
        public bool Timestamped { get; set; }
        public string Reason { get; set; }
        public string Level { get; set; }
        public string CitizenshipNo { get; set; }
        public string FullName { get; set; }

        public bool IsExpanded { get; set; }
        public int Index { get; set; }

        public string IssuerRDN { get; set; }

        public byte[] SerialNumber { get; set; }
        public string SerialNumberString
        {
            get
            {
                if (SerialNumber == null || SerialNumber.Length == 0)
                {
                    return null;
                }
                else
                {
                    return BitConverter.ToString(SerialNumber);
                }
            }
        }

        public byte[] SubjectKeyID { get; set; }

        public string SubjectKeyIDString
        {
            get
            {
                if (SubjectKeyID == null || SubjectKeyID.Length == 0)
                {
                    return null;
                }
                else
                {
                    return BitConverter.ToString(SubjectKeyID);
                }
            }
        }
    }

    public class VerifyDocumentResultTimestamp
    {
        public DateTime Time { get; set; }
        public string TSAName { get; set; }
        public int TimestampType { get; set; }
        public int Index { get; set; }

        public string IssuerRDN { get; set; }
        public byte[] SerialNumber { get; set; }

        public string SerialNumberString
        {
            get
            {
                if (SerialNumber == null || SerialNumber.Length == 0)
                {
                    return null;
                }
                else
                {
                    return BitConverter.ToString(SerialNumber);
                }
            }
        }

        public byte[] SubjectKeyID { get; set; }

        public string SubjectKeyIDString
        {
            get
            {
                if (SubjectKeyID == null || SubjectKeyID.Length == 0)
                {
                    return null;
                }
                else
                {
                    return BitConverter.ToString(SubjectKeyID);
                }
            }
        }
    }

}
