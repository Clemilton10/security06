using IdentityModel;
using IdentityServer.Models;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Quickstart.UI
{
	/// <summary>
	/// Este controlador de amostra implementa um fluxo de trabalho t�pico de login/logout/provis�o para contas locais e externas.
	/// O servi�o de login encapsula as intera��es com o armazenamento de dados do usu�rio.Este armazenamento de dados � apenas na mem�ria e n�o pode ser usado para produ��o!
	/// O Servi�o de Intera��o fornece uma maneira de a interface do usu�rio se comunicar com o IdentityServer para valida��o e recupera��o de contexto
	/// </summary>
	[SecurityHeaders]
	[AllowAnonymous]
	public class AccountController : Controller
	{
		//private readonly TestUserStore _users;
		private readonly IIdentityServerInteractionService _interaction;
		private readonly IClientStore _clientStore;
		private readonly IAuthenticationSchemeProvider _schemeProvider;
		private readonly IEventService _events;
		private readonly SignInManager<IdentityUser> _signInManager;
		private readonly UserManager<IdentityUser> _userManager;

		public AccountController(
			IIdentityServerInteractionService interaction,
			IClientStore clientStore,
			IAuthenticationSchemeProvider schemeProvider,
			IEventService events,
			SignInManager<IdentityUser> signInManager,
			UserManager<IdentityUser> userManager)
		{
			//_users = users ?? new TestUserStore(TestUsers.Users);

			_interaction = interaction;
			_clientStore = clientStore;
			_schemeProvider = schemeProvider;
			_events = events;
			_signInManager = signInManager;
			_userManager = userManager;
		}



		/// <summary>
		/// Registro para amostra de login de usu�rio
		/// </summary>
		/// <returns></returns>
		public IActionResult Register()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Register(RegisterModel model)
		{
			if (ModelState.IsValid)
			{
				var user = new IdentityUser
				{
					UserName = model.Email,
					Email = model.Email,
				};

				var result = await _userManager.CreateAsync(user, model.Password);

				if (result.Succeeded)
				{
					ViewBag.Success = "User successfuly added!!";

					return View();
				}

				foreach (var error in result.Errors)
				{
					ModelState.AddModelError("", error.Description);
				}

				ModelState.AddModelError(string.Empty, "Invalid Login Attempt");

			}
			return View(model);
		}

		/// <summary>
		/// Ponto de entrada no fluxo de trabalho de login
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Login(string returnUrl)
		{
			// Construa um modelo para sabermos o que mostrar na p�gina de login
			var vm = await BuildLoginViewModelAsync(returnUrl);

			if (vm.IsExternalLoginOnly)
			{
				// Temos apenas uma op��o para fazer login e � um provedor externo
				return RedirectToAction("Challenge", "External", new { scheme = vm.ExternalLoginScheme, returnUrl });
			}

			return View(vm);
		}

		/// <summary>
		/// lidera o postback do nome de usu�rio/login de senha
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginInputModel model, string button)
		{
			// Verifique se estamos no contexto de uma solicita��o de autoriza��o
			var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

			// O usu�rio clicou no bot�o "Cancelar"
			if (button != "login")
			{
				if (context != null)
				{
					// Se o usu�rio cancelar, envie um resultado de volta ao IdentityServer como se eles
					// negou o consentimento (mesmo que esse cliente n�o exija consentimento).
					// Isso enviar� de volta um acesso negou a resposta de erro OIDC ao cliente.
					await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

					// Podemos confiar no modelo.returnurl desde que o getAuthorizationContextasync retornou
					if (context.IsNativeClient())
					{
						// o cliente � nativo, ent�o essa mudan�a em como
						// retorna a resposta � para melhor UX para o usu�rio final.
						return this.LoadingPage("Redirect", model.ReturnUrl);
					}

					return Redirect(model.ReturnUrl);
				}
				else
				{
					// Como n�o temos um contexto v�lido, ent�o voltamos para a p�gina inicial
					return Redirect("~/");
				}
			}

			if (ModelState.IsValid)
			{
				var user = await _signInManager.UserManager.FindByNameAsync(model.Username);

				if (user is not null)
				{

					var userLogin = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);

					// Validar nome de usu�rio/senha contra a loja na mem�ria
					if (userLogin == Microsoft.AspNetCore.Identity.SignInResult.Success)
					{
						await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId));

						// Defina apenas a expira��o expl�cita aqui se o usu�rio escolher "Lembre -se de mim".
						// Caso contr�rio, contamos com a expira��o configurada no middleware de cookie.
						AuthenticationProperties props = null;
						if (AccountOptions.AllowRememberLogin && model.RememberLogin)
						{
							props = new AuthenticationProperties
							{
								IsPersistent = true,
								ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
							};
						};

						// emita um cookie de autentica��o com ID de assunto e nome de usu�rio
						var isuser = new IdentityServerUser(user.Id)
						{
							DisplayName = user.UserName
						};

						await HttpContext.SignInAsync(isuser, props);

						if (context != null)
						{
							if (context.IsNativeClient())
							{
								// o cliente � nativo, ent�o essa mudan�a em como
								// retorna a resposta � para melhor UX para o usu�rio final.
								return this.LoadingPage("Redirect", model.ReturnUrl);
							}

							// Podemos confiar no modelo.returnurl desde que getAuthorizationContextasync retornou n�o-nulo
							return Redirect(model.ReturnUrl);
						}

						// Solicita��o de uma p�gina local
						if (Url.IsLocalUrl(model.ReturnUrl))
						{
							return Redirect(model.ReturnUrl);
						}
						else if (string.IsNullOrEmpty(model.ReturnUrl))
						{
							return Redirect("~/");
						}
						else
						{
							// O usu�rio pode ter clicado em um link malicioso - deve ser registrado
							throw new Exception("invalid return URL");
						}
					}
				}


				await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials", clientId: context?.Client.ClientId));
				ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
			}

			// algo deu errado, mostre forma com erro
			var vm = await BuildLoginViewModelAsync(model);
			return View(vm);
		}


		/// <summary>
		/// Mostrar p�gina de logout
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Logout(string logoutId)
		{
			// Construa um modelo para que a p�gina de logout saiba o que exibir
			var vm = await BuildLogoutViewModelAsync(logoutId);

			if (vm.ShowLogoutPrompt == false)
			{
				// Se a solicita��o de logout foi autenticada adequadamente do IdentityServer, ent�o
				// N�o precisamos mostrar o prompt e podemos apenas registrar o usu�rio diretamente.
				return await Logout(vm);
			}

			return View(vm);
		}

		/// <summary>
		/// Lidar com o postback da p�gina de logout
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout(LogoutInputModel model)
		{
			// Construa um modelo para que a p�gina registrada saiba o que exibir
			var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

			if (User?.Identity.IsAuthenticated == true)
			{
				await _signInManager.SignOutAsync(); //identidade de sa�da

				// Excluir cookie de autentica��o local
				await HttpContext.SignOutAsync();

				// Aumente o evento de logout
				await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
			}

			// Verifique se precisamos acionar a sa�da em um provedor de identidade a montante
			if (vm.TriggerExternalSignout)
			{
				// Construa um URL de retorno para que o provedor a montante redirecione
				// para n�s depois que o usu�rio foi registrado.Isso nos permite ent�o
				// Preencha nosso processamento �nico de assinatura.
				string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

				// Isso desencadeia um redirecionamento para o provedor externo para sair
				return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
			}

			return View("LoggedOut", vm);
		}

		[HttpGet]
		public IActionResult AccessDenied()
		{
			return View();
		}


		/*****************************************/
		/* APIs auxiliares para o ContaController */
		/*****************************************/
		private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
		{
			var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
			if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
			{
				var local = context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

				// Isso pretende fazer o curto -circuito a interface do usu�rio e apenas acionar o One Externo IDP
				var vm = new LoginViewModel
				{
					EnableLocalLogin = local,
					ReturnUrl = returnUrl,
					Username = context?.LoginHint,
				};

				if (!local)
				{
					vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
				}

				return vm;
			}

			var schemes = await _schemeProvider.GetAllSchemesAsync();

			var providers = schemes
				.Where(x => x.DisplayName != null)
				.Select(x => new ExternalProvider
				{
					DisplayName = x.DisplayName ?? x.Name,
					AuthenticationScheme = x.Name
				}).ToList();

			var allowLocal = true;
			if (context?.Client.ClientId != null)
			{
				var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
				if (client != null)
				{
					allowLocal = client.EnableLocalLogin;

					if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
					{
						providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
					}
				}
			}

			return new LoginViewModel
			{
				AllowRememberLogin = AccountOptions.AllowRememberLogin,
				EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
				ReturnUrl = returnUrl,
				Username = context?.LoginHint,
				ExternalProviders = providers.ToArray()
			};
		}

		private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
		{
			var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
			vm.Username = model.Username;
			vm.RememberLogin = model.RememberLogin;
			return vm;
		}

		private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
		{
			var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

			if (User?.Identity.IsAuthenticated != true)
			{
				// Se o usu�rio n�o for autenticado, basta mostrar a p�gina logada
				vm.ShowLogoutPrompt = false;
				return vm;
			}

			var context = await _interaction.GetLogoutContextAsync(logoutId);
			if (context?.ShowSignoutPrompt == false)
			{
				// � seguro sair automaticamente
				vm.ShowLogoutPrompt = false;
				return vm;
			}

			// Mostra o prompt de logout.Isso evita ataques onde o usu�rio
			// � assinado automaticamente por outra p�gina da Web maliciosa.
			return vm;
		}

		private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
		{
			// Obtenha informa��es de contexto (nome do cliente, p�s -logout Redirecionar URI e IFRAME para federada FEDERATE)
			var logout = await _interaction.GetLogoutContextAsync(logoutId);

			var vm = new LoggedOutViewModel
			{
				AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
				PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
				ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
				SignOutIframeUrl = logout?.SignOutIFrameUrl,
				LogoutId = logoutId
			};

			if (User?.Identity.IsAuthenticated == true)
			{
				var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
				if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
				{
					var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
					if (providerSupportsSignout)
					{
						if (vm.LogoutId == null)
						{
							// Se n�o houver contexto de logout atual, precisamos criar um
							// Isso captura as informa��es necess�rias do usu�rio logado atual
							// Antes de se inscrever e redirecionar para o IDP externo para fazer a inscri��o
							vm.LogoutId = await _interaction.CreateLogoutContextAsync();
						}

						vm.ExternalAuthenticationScheme = idp;
					}
				}
			}

			return vm;
		}
	}
}