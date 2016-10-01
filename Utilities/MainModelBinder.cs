using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Web.Mvc;

namespace CRUDpanel.Utilities
{
    public class MainModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            object result;

            if (IsJSONRequest(controllerContext))
            {
                var request = controllerContext.HttpContext.Request;

                request.InputStream.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(request.InputStream))
                {
                    var jsonString = reader.ReadToEnd();

                    if (!String.IsNullOrWhiteSpace(jsonString))
                    {
                        var obj = JToken.Parse(jsonString);

                        if (obj != null && obj.HasValues && obj[bindingContext.ModelName] != null)
                        {
                            jsonString = obj[bindingContext.ModelName].ToString();
                        }
                    }

                    result = JsonConvert.DeserializeObject(jsonString, bindingContext.ModelType);
                }
            }
            else
            {
                result = base.BindModel(controllerContext, bindingContext);
            }

            return result;
        }

        private static bool IsJSONRequest(ControllerContext controllerContext)
        {
            var contentType = controllerContext.HttpContext.Request.ContentType;
            return contentType.Contains("application/json");
        }
    }
}