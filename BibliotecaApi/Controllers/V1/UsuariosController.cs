using AutoMapper;
using BibliotecaApi.Datos;
using BibliotecaApi.DTOs;
using BibliotecaApi.Entidades;
using BibliotecaApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BibliotecaApi.Controllers.V1
{
    [ApiController]
    [Route("api/v1/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<Usuario> signInManager;
        private readonly IServiciosUsuarios serviciosUsuarios;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public UsuariosController(UserManager<Usuario> userManager, IConfiguration configuration,
            SignInManager<Usuario> signInManager, IServiciosUsuarios serviciosUsuarios,
            ApplicationDbContext context, IMapper mapper)
        {
            _userManager = userManager;
            _configuration = configuration;
            this.signInManager = signInManager;
            this.serviciosUsuarios = serviciosUsuarios;
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet(Name = "ObtenerUsuariosV1")]
        [Authorize(Policy = "esadmin")]
        public async Task<IEnumerable<UsuarioDTO>> Get()
        {
            var usuarios = await context.Users.ToListAsync();
            var UsuarioDTOs = mapper.Map<List<UsuarioDTO>>(usuarios);   
            return UsuarioDTOs;
        }

        [HttpPost("registro", Name = "RegistrarUsuarioV1")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Registrar(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = new Usuario
            {
                UserName = credencialesUsuarioDTO.Email,
                Email = credencialesUsuarioDTO.Email
            };
            var resultado = await _userManager.CreateAsync(usuario, credencialesUsuarioDTO.Password!);
            if (resultado.Succeeded)
            {
                var respuestaAutenticacion = await ConstruirToken(credencialesUsuarioDTO);
                return respuestaAutenticacion;
            }
            else
            {
                foreach ( var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return ValidationProblem();
        }
        [HttpPost("login", Name = "LoginUsuarioV1")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Login(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = await _userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);

            if (usuario is null)
            {
                return RetornarLoginIncorrecto();
            }

            var resultado = await signInManager.CheckPasswordSignInAsync(usuario, credencialesUsuarioDTO.Password!,
                lockoutOnFailure: false);

            if (resultado.Succeeded)
            {
                return await ConstruirToken(credencialesUsuarioDTO);
            }
            else
            {
                return RetornarLoginIncorrecto();
            }
        }

        [HttpPut(Name = "ActualizarUsuarioV1")]
        [Authorize]
        public async Task<ActionResult> Put(ActualizarUsuarioDTO actualizarUsuarioDTO)
        {
            var usuario = await serviciosUsuarios.ObtenerUsuario();

            if (usuario is null)
            {
                return NotFound();
            }

            usuario.FechaNacimiento = actualizarUsuarioDTO.FechaNacimiento;

            await _userManager.UpdateAsync(usuario);
            return NoContent();
        }

        [HttpGet("renovar-token", Name = "RenovarTokenUsuarioV1")]
        [Authorize]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> RenovarToken()
        {
            var usuario = await serviciosUsuarios.ObtenerUsuario();
            if (usuario is null)
            {
                return NotFound();
            }

            var credencialesUsuarioDTO = new CredencialesUsuarioDTO 
            { 
                Email = usuario.Email! 
            };

            var respuestaAutenticacion = await ConstruirToken(credencialesUsuarioDTO);
            return respuestaAutenticacion;
        }

        [HttpPost("hacer-admin", Name = "HacerAdminUsuarioV1")]
        [Authorize(Policy = "esadmin")]
        public async Task<ActionResult> HacerAdmin(EditarClaimDTO editarClaimDTO)
        {
            var usuario = await _userManager.FindByEmailAsync(editarClaimDTO.Email);
            if (usuario is null)
            {
                return NotFound();
            }
            await _userManager.AddClaimAsync(usuario, new Claim("esadmin", "true"));
            return NoContent();
        }

        [HttpPost("remover-admin", Name = "RemoverAdminUsuarioV1")]
        [Authorize(Policy = "esadmin")]
        public async Task<ActionResult> RemoverAdmin(EditarClaimDTO editarClaimDTO)
        {
            var usuario = await _userManager.FindByEmailAsync(editarClaimDTO.Email);
            if (usuario is null)
            {
                return NotFound();
            }
            await _userManager.RemoveClaimAsync(usuario, new Claim("esadmin", "true"));
            return NoContent();
        }

        private ActionResult RetornarLoginIncorrecto()
        {
            ModelState.AddModelError(string.Empty, "Login incorrecto");
            return ValidationProblem();
        }

        private async Task<RespuestaAutenticacionDTO> ConstruirToken(CredencialesUsuarioDTO credencialesUsuarioDTO)
        { 
            var claims = new List<Claim>
            {
                new Claim("email", credencialesUsuarioDTO.Email),
                new Claim("lo que yo quiera", "cualquier valor")
            };

            var usuario = await _userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);
            var claimsDB = await _userManager.GetClaimsAsync(usuario!);

            claims.AddRange(claimsDB);

            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["llavejwt"]!));
            var credenciales = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);

            var expiracion = DateTime.UtcNow.AddYears(1);

            var tokenDeSeguridad = new JwtSecurityToken(issuer: null, audience: null,
                claims: claims, expires: expiracion, signingCredentials: credenciales);

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDeSeguridad);
            return new RespuestaAutenticacionDTO { Token = token, Expiracion = expiracion };
        }
    }
}
