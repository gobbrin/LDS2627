package pt.ipca.g15.campuscarpool.ui
import android.net.Uri
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.key
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavController
import androidx.navigation.NavGraphBuilder
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import kotlinx.coroutines.flow.StateFlow
import pt.ipca.g15.campuscarpool.R
import pt.ipca.g15.campuscarpool.domain.model.LigacaoApp
import pt.ipca.g15.campuscarpool.ui.autenticacao.ConfirmacaoScreen
import pt.ipca.g15.campuscarpool.ui.autenticacao.FaseConfirmacao
import pt.ipca.g15.campuscarpool.ui.autenticacao.LoginScreen
import pt.ipca.g15.campuscarpool.ui.autenticacao.LoginViewModel
import pt.ipca.g15.campuscarpool.ui.autenticacao.ReenvioScreen
import pt.ipca.g15.campuscarpool.ui.autenticacao.RegistoScreen
import pt.ipca.g15.campuscarpool.ui.autenticacao.RegistoViewModel
import pt.ipca.g15.campuscarpool.ui.inicio.InicioScreen
import pt.ipca.g15.campuscarpool.ui.perfil.EditarPerfilScreen
import pt.ipca.g15.campuscarpool.ui.perfil.PerfilPublicoScreen
import pt.ipca.g15.campuscarpool.ui.perfil.PerfilScreen
import pt.ipca.g15.campuscarpool.ui.perfil.PerfilViewModel
import pt.ipca.g15.campuscarpool.ui.viagens.CriarViagemScreen
import pt.ipca.g15.campuscarpool.ui.viagens.GerirViagemScreen
import pt.ipca.g15.campuscarpool.ui.viagens.MinhasViagensScreen
import pt.ipca.g15.campuscarpool.ui.viagens.MinhasViagensViewModel
import pt.ipca.g15.campuscarpool.ui.viagens.ViagemFormViewModel

private const val INICIO = "inicio"
private const val ENTRADA = "login"
private const val REGISTO = "registo"
private const val CONFIRMAR = "confirmar"
private const val REENVIO = "reenvio"
private const val PERFIL = "perfil"
private const val EDITAR_PERFIL = "perfil/editar"
private const val UTILIZADOR = "utilizador"
private const val VIAGENS = "viagens"
private const val NOVA_VIAGEM = "viagens/nova"

private fun NavController.abrirReenvio(email: String = "") =
    navigate("$REENVIO?email=${Uri.encode(email)}")

private fun NavController.abrirEntrada() = navigate(ENTRADA) {
    popUpTo(INICIO)
    launchSingleTop = true
}

@Composable fun AppNavigation(
    login: LoginViewModel,
    online: StateFlow<Boolean>,
    fabrica: ViewModelProvider.Factory
) {
    val sessao by login.sessao.collectAsStateWithLifecycle()
    val estado by login.estado.collectAsStateWithLifecycle()
    val comRede by online.collectAsStateWithLifecycle()
    val ligacao by login.ligacaoPendente.collectAsStateWithLifecycle()
    // Mudar de sessão recria o grafo, eliminando destinos privados da pilha de Voltar.
    key(sessao?.accessToken) {
        val nav = rememberNavController()
        val destino by nav.currentBackStackEntryAsState()
        Scaffold { padding ->
            Column(Modifier.fillMaxSize().padding(padding)) {
                if (!comRede) {
                    Surface {
                        Text(
                            stringResource(R.string.sem_rede),
                            Modifier.fillMaxWidth().padding(12.dp).semantics {
                                liveRegion =
                                    LiveRegionMode.Polite
                            }
                        )
                    }
                }
                estado.aviso?.let { mensagem ->
                    Surface {
                        Column(Modifier.padding(12.dp)) {
                            Text(
                                mensagem,
                                Modifier.semantics {
                                    liveRegion =
                                        LiveRegionMode.Assertive
                                }
                            )
                            TextButton(onClick = login::limparAviso) {
                                Text(stringResource(R.string.fechar))
                            }
                        }
                    }
                }
                NavHost(
                    navController = nav,
                    startDestination = INICIO,
                    modifier = Modifier.weight(1f)
                ) {
                    composable(INICIO) {
                        InicioScreen(
                            sessao != null,
                            { nav.navigate(ENTRADA) },
                            { nav.navigate(REGISTO) },
                            { nav.navigate(PERFIL) },
                            { nav.navigate(VIAGENS) },
                            login::terminarSessao,
                            estado.ocupado
                        )
                    }
                    rotaConfirmacao(nav, fabrica, sessao != null)
                    if (sessao == null) {
                        rotasDeVisitante(nav, fabrica, login)
                    } else {
                        rotasDePerfil(nav, fabrica, login)
                        rotasDeViagens(nav, fabrica, login)
                    }
                }
                // Dentro da mesma composição do NavHost: o grafo já existe quando isto corre.
                LaunchedEffect(ligacao, sessao != null) {
                    when (val pedida = ligacao) {
                        is LigacaoApp.Confirmar -> {
                            login.consumirLigacao()
                            nav.navigate("$CONFIRMAR?token=${Uri.encode(pedida.token)}")
                        }
                        is LigacaoApp.Perfil -> if (sessao != null) {
                            login.consumirLigacao()
                            nav.navigate("$UTILIZADOR/${pedida.id}")
                        } else {
                            nav.abrirEntrada()
                        }
                        null -> Unit
                    }
                }
                LaunchedEffect(estado.irParaEntrada, sessao) {
                    if (estado.irParaEntrada && sessao == null) {
                        login.consumirIdaParaEntrada()
                        nav.abrirEntrada()
                    }
                }
                LaunchedEffect(destino?.destination?.route) {
                    if (destino?.destination?.route != ENTRADA) login.limparErroEntrada()
                }
            }
        }
    }
}

private fun NavGraphBuilder.rotaConfirmacao(
    nav: NavHostController,
    fabrica: ViewModelProvider.Factory,
    autenticado: Boolean
) {
    // Fora das rotas de visitante: a ligação do email tem de funcionar mesmo com sessão aberta.
    composable(
        "$CONFIRMAR?token={token}",
        arguments = listOf(
            navArgument("token") {
                type = NavType.StringType
                nullable = true
                defaultValue = null
            }
        )
    ) { entrada ->
        val vm = viewModel<RegistoViewModel>(factory = fabrica)
        val estado by vm.confirmacao.collectAsStateWithLifecycle()
        val token = entrada.arguments?.getString("token")
        // A fase garante que rodar o ecrã não repete o pedido com a mesma ligação.
        LaunchedEffect(token) {
            if (token != null && vm.confirmacao.value.fase == FaseConfirmacao.INICIAL) {
                vm.confirmar(token)
            }
        }
        ConfirmacaoScreen(
            estado,
            autenticado,
            vm::confirmarTexto,
            vm::repetirConfirmacao,
            { nav.abrirReenvio() },
            { nav.abrirEntrada() },
            { nav.popBackStack() }
        )
    }
}

private fun NavGraphBuilder.rotasDeVisitante(
    nav: NavHostController,
    fabrica: ViewModelProvider.Factory,
    login: LoginViewModel
) {
    composable(ENTRADA) {
        val estado by login.estado.collectAsStateWithLifecycle()
        LoginScreen(
            estado,
            login::entrar,
            { nav.popBackStack() },
            { nav.navigate(REGISTO) },
            { nav.abrirReenvio(it) },
            { nav.navigate(CONFIRMAR) }
        )
    }
    composable(REGISTO) {
        val vm = viewModel<RegistoViewModel>(factory = fabrica)
        val estado by vm.estado.collectAsStateWithLifecycle()
        RegistoScreen(
            estado,
            vm::registar,
            { nav.abrirEntrada() },
            { nav.abrirReenvio(it) },
            { nav.navigate(CONFIRMAR) },
            { nav.popBackStack() }
        )
    }
    composable(
        "$REENVIO?email={email}",
        arguments = listOf(
            navArgument("email") {
                type = NavType.StringType
                defaultValue = ""
            }
        )
    ) { entrada ->
        val vm = viewModel<RegistoViewModel>(factory = fabrica)
        val estado by vm.reenvio.collectAsStateWithLifecycle()
        ReenvioScreen(
            estado,
            entrada.arguments?.getString("email").orEmpty(),
            vm::reenviar,
            { nav.navigate(CONFIRMAR) },
            { nav.abrirEntrada() },
            { nav.popBackStack() }
        )
    }
}

private fun NavGraphBuilder.rotasDePerfil(
    nav: NavHostController,
    fabrica: ViewModelProvider.Factory,
    login: LoginViewModel
) {
    composable(PERFIL) {
        val vm = viewModel<PerfilViewModel>(factory = fabrica)
        val estado by vm.estado.collectAsStateWithLifecycle()
        // Corre sempre que se volta aqui, por isso o perfil mostrado vem do servidor.
        LaunchedEffect(Unit) { vm.carregarProprio() }
        PerfilScreen(
            estado,
            vm::carregarProprio,
            { nav.navigate(EDITAR_PERFIL) },
            { nav.navigate("$UTILIZADOR/$it") },
            { nav.popBackStack() }
        )
    }
    composable(EDITAR_PERFIL) {
        val vm = viewModel<PerfilViewModel>(factory = fabrica)
        val estado by vm.estado.collectAsStateWithLifecycle()
        val perfilAtualizado = stringResource(R.string.perfil_atualizado)
        LaunchedEffect(Unit) { if (vm.estado.value.perfil == null) vm.carregarProprio() }
        LaunchedEffect(estado.guardado) {
            if (estado.guardado) {
                vm.consumirGuardado()
                login.mostrarAviso(perfilAtualizado)
                nav.popBackStack()
            }
        }
        EditarPerfilScreen(
            estado,
            vm::carregarProprio,
            vm::guardar,
            vm::adicionarLigacao,
            vm::removerLigacao,
            { nav.popBackStack() }
        )
    }
    composable(
        "$UTILIZADOR/{id}",
        arguments = listOf(navArgument("id") { type = NavType.StringType })
    ) { entrada ->
        val vm = viewModel<PerfilViewModel>(factory = fabrica)
        val estado by vm.publico.collectAsStateWithLifecycle()
        val id = entrada.arguments?.getString("id").orEmpty()
        LaunchedEffect(id) { vm.carregarPublico(id) }
        PerfilPublicoScreen(estado, { vm.carregarPublico(id) }, { nav.popBackStack() })
    }
}

private fun NavGraphBuilder.rotasDeViagens(
    nav: NavHostController,
    fabrica: ViewModelProvider.Factory,
    login: LoginViewModel
) {
    composable(VIAGENS) {
        val vm = viewModel<MinhasViagensViewModel>(factory = fabrica)
        val estado by vm.estado.collectAsStateWithLifecycle()
        // Corre sempre que se volta aqui: o estado e os lugares vêm do servidor.
        LaunchedEffect(Unit) { vm.carregar() }
        MinhasViagensScreen(
            estado,
            vm::carregar,
            { nav.navigate(NOVA_VIAGEM) },
            { nav.navigate("$VIAGENS/$it/editar") },
            { nav.popBackStack() }
        )
    }
    composable(NOVA_VIAGEM) {
        val vm = viewModel<ViagemFormViewModel>(factory = fabrica)
        val estado by vm.estado.collectAsStateWithLifecycle()
        LaunchedEffect(estado.aviso) {
            estado.aviso?.let {
                vm.consumirAviso()
                login.mostrarAviso(it)
            }
        }
        LaunchedEffect(estado.criada) {
            if (estado.criada) {
                vm.consumirCriada()
                nav.popBackStack()
            }
        }
        CriarViagemScreen(estado, vm::criar) { nav.popBackStack() }
    }
    composable(
        "$VIAGENS/{id}/editar",
        arguments = listOf(navArgument("id") { type = NavType.StringType })
    ) { entrada ->
        val vm = viewModel<ViagemFormViewModel>(factory = fabrica)
        val estado by vm.estado.collectAsStateWithLifecycle()
        val id = entrada.arguments?.getString("id").orEmpty()
        LaunchedEffect(id) { vm.abrir(id) }
        LaunchedEffect(estado.aviso) {
            estado.aviso?.let {
                vm.consumirAviso()
                login.mostrarAviso(it)
            }
        }
        GerirViagemScreen(
            estado,
            { vm.carregar(id) },
            { vm.alterar(id, it) },
            { vm.indisponibilizar(id) },
            { nav.popBackStack() }
        )
    }
}
