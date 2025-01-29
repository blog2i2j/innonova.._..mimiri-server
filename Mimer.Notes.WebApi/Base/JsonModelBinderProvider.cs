using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Mimer.Framework.Json;

namespace Mimer.Notes.WebApi.Base {
	public class JsonModelBinderProvider : IModelBinderProvider {
		public IModelBinder? GetBinder(ModelBinderProviderContext context) {
			if (context.Metadata.ModelType == typeof(JsonObject)) {
				return new BinderTypeModelBinder(typeof(JsonModelBinder));
			}
			return null;
		}
	}
}
