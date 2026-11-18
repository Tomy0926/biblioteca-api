using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace BibliotecaApi.Swagger
{
    public class ConvencionAgrupaPorVersion : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            // Ejemplo: "ControllersV1
            var namespaceDelControlador = controller.ControllerType.Namespace; // Controllers.V1
            var version = namespaceDelControlador!.Split('.').Last().ToLower(); // v1
            controller.ApiExplorer.GroupName = version; // Agrupa por versión
        }
    }
}
