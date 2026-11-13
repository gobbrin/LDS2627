package pt.ipca.g15.campuscarpool.ui
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.key
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import pt.ipca.g15.campuscarpool.R
import pt.ipca.g15.campuscarpool.ui.autenticacao.LoginScreen
import pt.ipca.g15.campuscarpool.ui.inicio.InicioScreen
import pt.ipca.g15.campuscarpool.ui.perfil.PerfilScreen

@Composable fun AppNavigation(vm: AppViewModel) {
    val sessao by vm.sessao.collectAsStateWithLifecycle()
    val estado by vm.estado.collectAsStateWithLifecycle()
    val online by vm.online.collectAsStateWithLifecycle()
    // Mudar de sessão recria o grafo, eliminando destinos privados da pilha de Voltar.
    key(sessao?.accessToken) {
        val nav = rememberNavController()
        Scaffold { padding ->
            Column(Modifier.fillMaxSize().padding(padding)) {
                if (!online) {
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
                estado.mensagem?.let { mensagem ->
                    Surface {
                        Column(Modifier.padding(12.dp)) {
                            Text(
                                mensagem,
                                Modifier.semantics {
                                    liveRegion =
                                        LiveRegionMode.Assertive
                                }
                            )
                            TextButton(onClick = vm::limparMensagem) {
                                Text(stringResource(R.string.fechar))
                            }
                        }
                    }
                }
                NavHost(
                    navController = nav,
                    startDestination = "inicio",
                    modifier = Modifier.weight(1f)
                ) {
                    composable("inicio") {
                        InicioScreen(sessao != null, {
                            nav.navigate("login")
                        }, { nav.navigate("perfil") }, vm::sair, estado.ocupado)
                    }
                    if (sessao == null) {
                        composable("login") {
                            LoginScreen(estado.ocupado, estado.campos, vm::entrar, {
                                nav.popBackStack()
                            })
                        }
                    }
                    if (sessao != null) {
                        composable("perfil") {
                            PerfilScreen(
                                estado.perfil,
                                estado.aCarregarPerfil,
                                vm::carregarPerfil,
                                {
                                    nav.popBackStack()
                                }
                            )
                        }
                    }
                }
            }
        }
    }
}
