using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mimer.Framework.Json;
using System.Text;

namespace Mimer.Notes.WebApi.Base {
	public class JsonModelBinder : IModelBinder {
		public async Task BindModelAsync(ModelBindingContext bindingContext) {
			using var reader = new StreamReader(bindingContext.ActionContext.HttpContext.Request.Body, Encoding.UTF8);
			bindingContext.Result = ModelBindingResult.Success(new JsonObject(await reader.ReadToEndAsync()));
		}
	}
}
