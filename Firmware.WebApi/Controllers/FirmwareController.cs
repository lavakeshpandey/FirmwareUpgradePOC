﻿using Firmware.IBL;
using Firmware.Model.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Firmware.WebApi.Controllers
{

    public class FirmwareController : ApiController
    {
        private readonly IFirmwareRepository _repository;

        public FirmwareController(IFirmwareRepository repository)
        {
            this._repository = repository;
        }
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [HttpPost, Route("api/UploadSoftwarePackage")]
        public async Task<IHttpActionResult> UploadSoftwarePackage(string guid)
        {
            var key = guid.Trim('\"');

            if (String.IsNullOrEmpty(key) || !Guid.TryParse(key, out _))
                return base.Content(HttpStatusCode.BadRequest, "Bad request.", new JsonMediaTypeFormatter(), "text/plain");

            try
            {
                var count = HttpContext.Current.Request.Files.Count;
                var beforeUpload = GC.GetTotalMemory(false);
                string id = string.Empty;
                if (!Request.Content.IsMimeMultipartContent())
                {
                    throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                }
                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                byte[] fristBuffer = null, swPackgBuffer = null;
                byte[] secondBuffer = null, helpDocBuffer = null;

                string firstFileName = string.Empty, swPackgFilename = string.Empty;
                string secondFileName = string.Empty, helpDocFilename = string.Empty;

                var firstFile = provider.Contents[0];
                firstFileName = firstFile.Headers.ContentDisposition.FileName.Trim('\"');
                fristBuffer = await firstFile.ReadAsByteArrayAsync();

                if (count > 1)
                {
                    var secondFile = provider.Contents[1];
                    secondFileName = secondFile.Headers.ContentDisposition.FileName.Trim('\"');
                    secondBuffer = await secondFile.ReadAsByteArrayAsync();
                }

                var arr = firstFileName.Split('.');
                var extension = arr[arr.Length - 1];

                if (extension == "bin" || extension == "pkg")
                {
                    swPackgBuffer = fristBuffer;
                    swPackgFilename = firstFileName;
                    helpDocBuffer = secondBuffer;
                    helpDocFilename = secondFileName;
                }
                else if (extension == "pdf" || extension == "doc" || extension == "docx")
                {
                    swPackgBuffer = secondBuffer;
                    swPackgFilename = secondFileName;
                    helpDocBuffer = fristBuffer;
                    helpDocFilename = firstFileName;
                }

                id = _repository.UploadFirmware(swPackgBuffer, swPackgFilename, helpDocBuffer, helpDocFilename, key).ToString();
                return base.Content(HttpStatusCode.OK, id, new JsonMediaTypeFormatter(), "text/plain"); ;
            }
            catch (Exception)
            {
                return base.Content(HttpStatusCode.InternalServerError, "Internal Server Error.", new JsonMediaTypeFormatter(), "text/plain");
            }

        }
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [HttpPost, Route("api/AddSoftwarePackage")]
        public async Task<IHttpActionResult> AddSoftwarePackage(SoftwarePackageAdd softwarePackage)
        {
            if (softwarePackage == null || String.IsNullOrEmpty(softwarePackage.Key) || !Guid.TryParse(softwarePackage.Key.Trim('\"'), out _))
                return base.Content(HttpStatusCode.BadRequest, "Bad request.", new JsonMediaTypeFormatter(), "text/plain");

            var result = false;
            try
            {
                var key = softwarePackage.Key.Trim('\"');

                result = await Task.Run(() => _repository.AddFirmware(key, softwarePackage.SwPkgVersion, softwarePackage.SwPkgDescription, softwarePackage.SwColorStandardID, softwarePackage.SwFileChecksum, softwarePackage.SwFileChecksumType, softwarePackage.SwCreatedBy, softwarePackage.Manufacturer, softwarePackage.DeviceType, softwarePackage.SupportedModels, softwarePackage.BlobDescription));

                return base.Content(HttpStatusCode.Created, result, new JsonMediaTypeFormatter(), "text/plain"); ;
            }
            catch (Exception)
            {
                return base.Content(HttpStatusCode.InternalServerError, result, new JsonMediaTypeFormatter(), "text/plain"); ;
            }

        }
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [HttpPost, Route("api/CancelUpload")]
        public async Task<IHttpActionResult> CancelUpload(string key)
        {
            key = key.Trim('\"');

            if (String.IsNullOrEmpty(key) || !Guid.TryParse(key, out _))
                return base.Content(HttpStatusCode.BadRequest, "Bad request.", new JsonMediaTypeFormatter(), "text/plain");
            try
            {
                var result = await Task.Run(() => _repository.DeleteSwPackageFromMemory(key));

                return base.Content(HttpStatusCode.OK, result, new JsonMediaTypeFormatter(), "text/plain");
            }
            catch (Exception)
            {
                return base.Content(HttpStatusCode.InternalServerError, "Internal Server Error.", new JsonMediaTypeFormatter(), "text/plain"); ;
            }

        }
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [HttpGet, Route("api/GetAllSoftwarePackage")]
        public async Task<IHttpActionResult> GetAllSoftwarePackage(int pageNo, int pageSize, string searchText, string sortColumn, string sortDirection)
        {
            try
            {
                var result = await Task.Run(() => _repository.GetAllSoftwarePackage(pageNo, pageSize, searchText, sortColumn, sortDirection));

                return base.Content(HttpStatusCode.OK, result, new JsonMediaTypeFormatter(), "text/plain");
            }
            catch (Exception)
            {
                return base.Content(HttpStatusCode.InternalServerError, "Internal Server Error.", new JsonMediaTypeFormatter(), "text/plain");
            }

        }
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [HttpPost, Route("api/DeleteSoftwarePackage")]
        public async Task<IHttpActionResult> DeleteSoftwarePackage(DeleteSwPackageModel swPackageModel)
        {
            bool result = false;

            if (swPackageModel != null)
            {

                result = await Task.Run(() => _repository.DeleteSoftwarePackage(swPackageModel.DeleteAll ? new List<Guid> { Guid.Empty } : swPackageModel.PackageIds, swPackageModel.DeleteAll));
                if (result)
                {
                    return base.Content(HttpStatusCode.OK, result, new JsonMediaTypeFormatter(), "text/plain");
                }
                else
                {
                    return base.Content(HttpStatusCode.InternalServerError, result, new JsonMediaTypeFormatter(), "text/plain");
                }
            }
            else
            {
                return base.Content(HttpStatusCode.BadRequest, result, new JsonMediaTypeFormatter(), "text/plain");
            }
        }

        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [HttpGet, Route("api/GetModels")]
        public async Task<IHttpActionResult> GetModels()
        {
            try
            {
                var result = await Task.Run(() => _repository.GetCameraModels());
                return base.Content(HttpStatusCode.OK, result, new JsonMediaTypeFormatter(), "text/plain");
            }
            catch (Exception)
            {
                return base.Content(HttpStatusCode.InternalServerError, "Internal Server Error.", new JsonMediaTypeFormatter(), "text/plain");
            }

        }

        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [HttpGet, Route("api/GetHelpDoc")]
        public async Task<IHttpActionResult> GetHelpDoc(string key)
        {
            key = key.Trim('\"');

            if (String.IsNullOrEmpty(key) || !Guid.TryParse(key, out _))
                return base.Content(HttpStatusCode.BadRequest, "Bad request.", new JsonMediaTypeFormatter(), "text/plain");

            try
            {
                var result = await Task.Run(() => _repository.GetHelpDoc(key));

                return base.Content(HttpStatusCode.OK, result, new JsonMediaTypeFormatter(), "application/octet-stream");
            }
            catch (Exception)
            {
                return base.Content(HttpStatusCode.InternalServerError, "Internal Server Error.", new JsonMediaTypeFormatter(), "text/plain");
            }
            
        }
    }
}
