using System.ComponentModel.DataAnnotations;
using BibliotecaApi.Validaciones;

namespace BibliotecaApiTest.PruebasUnitarias.Validaciones
{
    [TestClass]
    public class PrimeraLetraMayusculaAttributePruebas
    {
        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow(null)]
        [DataRow("Tomy")]
        public void IsValid_ReturnaExitoso_SiValueNotieneLaPrimeraLetraMinuscula(string value)
        {
            // Preparación

            var primeraLetraMayusculaAttribute = new PrimeraLetraMayusculaAttribute();
            var validationContext = new ValidationContext(new object());

            // Prueba

            var resultado = primeraLetraMayusculaAttribute.GetValidationResult(value, validationContext);

            //Verificación
            Assert.AreEqual(expected: ValidationResult.Success, actual: resultado);
        }

        [TestMethod]
        [DataRow("tomy")]
        public void IsValid_ReturnaError_SiValueTieneLaPrimeraLetraMinuscula(string value)
        {
            // Preparación

            var primeraLetraMayusculaAttribute = new PrimeraLetraMayusculaAttribute();
            var validationContext = new ValidationContext(new object());

            // Prueba

            var resultado = primeraLetraMayusculaAttribute.GetValidationResult(value, validationContext);

            //Verificación
            Assert.AreEqual(expected: "La primera letra debe ser mayúscula", actual: resultado!.ErrorMessage);
        }
    }
}
