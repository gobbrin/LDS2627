package pt.ipca.g15.campuscarpool.ui.comunidade
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.google.gson.JsonObject
import pt.ipca.g15.campuscarpool.di.AppContainer
class MensagensVm(app: AppContainer) : ComunidadeVm(app) {
    var conversas by mutableStateOf(emptyList<JsonObject>())
    var mensagens by mutableStateOf(emptyList<JsonObject>())
    var destinatario by mutableStateOf("")
    var nome by mutableStateOf("")
    fun listar() {
        pedir({ api.conversas() }) {
            conversas =
                it
        }
    }
    fun abrir(id: String, n: String) {
        if (ocupado)return
        destinatario = id
        nome = n
        mensagens =
            emptyList()
        pedir({ api.mensagens(id) }) { mensagens = it }
    }
    fun enviar(conteudo: String, limpar: () -> Unit) {
        if (conteudo.isBlank() ||
            conteudo.length > 1000 ||
            destinatario == app.sessao.estado.value?.utilizadorId
        ) {
            return
        }
        pedir({
            api.mensagem(
                corpo(
                    "destinatarioId" to destinatario,
                    "conteudo" to conteudo.trim()
                )
            )
        }) {
            limpar()
            abrir(destinatario, nome)
        }
    }
}

@Composable fun MensagensScreen(inicial: String? = null) {
    val vm: MensagensVm = obterVm { MensagensVm(it) }
    val sessaoAtual by vm.app.sessao.estado.collectAsStateWithLifecycle()
    var conteudo by rememberSaveable { mutableStateOf("") }
    LaunchedEffect(inicial) {
        if (inicial !=
            null
        ) {
            vm.abrir(inicial, "Condutor")
        } else {
            vm.listar()
        }
    }
    Text("Mensagens individuais", style = MaterialTheme.typography.headlineSmall)
    Estado(vm)
    Botao("Atualizar conversas", !vm.ocupado) { vm.listar() }
    vm.conversas.forEach { c ->
        Botao("${c.texto("outroUtilizadorNome")} · ${c.texto("ultimaMensagem")}", !vm.ocupado) {
            vm.abrir(c.texto("outroUtilizadorId"), c.texto("outroUtilizadorNome"))
        }
    }
    if (vm.destinatario.isNotBlank()) {
        Text(vm.nome)
        Botao("Atualizar mensagens", !vm.ocupado) { vm.abrir(vm.destinatario, vm.nome) }
        vm.mensagens.forEach { m ->
            Bloco {
                Text(
                    if (m.texto("remetenteId") ==
                        sessaoAtual?.utilizadorId
                    ) {
                        "Tu"
                    } else {
                        vm.nome
                    }
                )
                Text(m.texto("conteudo"))
                Text(dataPt(m.texto("enviadoEm")))
            }
        }
        OutlinedTextField(value = conteudo, onValueChange = {
            if (it.length <=
                1000
            ) {
                conteudo = it
            }
        }, label = { Text("Mensagem") })
        Botao(
            "Enviar",
            !vm.ocupado && conteudo.isNotBlank()
        ) { vm.enviar(conteudo) { conteudo = "" } }
    }
}
class AvisosVm(app: AppContainer) : ComunidadeVm(app) {
    var avisos by mutableStateOf(emptyList<JsonObject>())
    fun carregar(id: String) {
        pedir({ api.avisos(id) }) {
            avisos =
                it
        }
    }
    fun enviar(id: String, conteudo: String, limpar: () -> Unit) {
        if (conteudo.isBlank() ||
            conteudo.length > 100
        ) {
            return
        }
        pedir({ api.aviso(id, corpo("conteudo" to conteudo.trim())) }) {
            limpar()
            carregar(id)
        }
    }
}

@Composable fun AvisosScreen(id: String, condutor: Boolean) {
    val vm: AvisosVm = obterVm { AvisosVm(it) }
    var conteudo by rememberSaveable(id) { mutableStateOf("") }
    LaunchedEffect(id) { vm.carregar(id) }
    Text("Avisos da viagem", style = MaterialTheme.typography.headlineSmall)
    Estado(vm)
    Botao("Atualizar avisos", !vm.ocupado) { vm.carregar(id) }
    if (condutor) {
        OutlinedTextField(value = conteudo, onValueChange = {
            if (it.length <=
                100
            ) {
                conteudo = it
            }
        }, label = { Text("Aviso aos passageiros aceites") })
        Botao(
            "Publicar",
            !vm.ocupado && conteudo.isNotBlank()
        ) { vm.enviar(id, conteudo) { conteudo = "" } }
    }
    vm.avisos.forEach { a ->
        Bloco {
            Text(a.texto("conteudo"))
            Text(dataPt(a.texto("criadoEm")))
        }
    }
}
