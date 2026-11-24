package pt.ipca.g15.campuscarpool.ui.comunidade
import android.Manifest
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import com.google.gson.JsonObject
import pt.ipca.g15.campuscarpool.di.AppContainer
class NotificacoesVm(app: AppContainer) : ComunidadeVm(app) {
    var caixa by mutableStateOf<JsonObject?>(null)
    var pagina by mutableStateOf(1)
    fun carregar(p: Int = pagina) {
        pedir({ api.notificacoes(p) }) {
            caixa =
                it
            pagina = p
        }
    }
    fun ler(id: String) {
        acao({ api.ler(id) }) { carregar() }
    }
    fun todas() {
        pedir({ api.lerTodas() }) { carregar() }
    }
}

@Composable fun NotificacoesScreen() {
    val vm: NotificacoesVm = obterVm { NotificacoesVm(it) }
    var mensagem by remember { mutableStateOf("") }
    val permissao =
        rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { ok ->
            if (ok) {
                vm.app.push.ativar()
                mensagem =
                    "Preferência guardada. As atualizações continuam disponíveis nesta caixa."
            } else {
                mensagem =
                    "Sem permissão: as notificações continuam disponíveis nesta caixa."
            }
        }
    LaunchedEffect(Unit) {
        vm.carregar()
        EventosPedido.alteracoes.collect { vm.carregar() }
    }
    Text("Notificações", style = MaterialTheme.typography.headlineSmall)
    Estado(vm)
    Text(mensagem)
    Botao("Ativar push de pedidos") {
        if (Build.VERSION.SDK_INT >=
            33
        ) {
            permissao.launch(Manifest.permission.POST_NOTIFICATIONS)
        } else {
            vm.app.push.ativar()
            mensagem =
                "Preferência guardada."
        }
    }
    Botao("Desativar push") {
        vm.app.push.desativar()
        mensagem =
            "Push desativado neste dispositivo."
    }
    Botao("Atualizar", !vm.ocupado) { vm.carregar() }
    Botao("Marcar todas como lidas", !vm.ocupado) { vm.todas() }
    vm.caixa?.let { c ->
        Text("${c.texto("naoLidas")} por ler")
        c.lista("itens").forEach { n ->
            Bloco {
                Text(n.texto("titulo"))
                Text(n.texto("conteudo"))
                Text(dataPt(n.texto("criadaEm")))
                if (n.texto("lidaEm").isBlank()) {
                    Botao("Marcar como lida", !vm.ocupado) {
                        vm.ler(n.texto("id"))
                    }
                }
            }
        }
        Botao(
            "Anterior",
            !vm.ocupado && vm.pagina > 1
        ) { vm.carregar(vm.pagina - 1) }
        Botao(
            "Seguinte",
            !vm.ocupado && vm.pagina < (vm.caixa?.numero("totalPaginas") ?: 0.0)
        ) {
            vm.carregar(
                vm.pagina + 1
            )
        }
    }
}
