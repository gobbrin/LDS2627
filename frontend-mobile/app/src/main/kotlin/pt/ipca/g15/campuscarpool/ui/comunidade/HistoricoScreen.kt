package pt.ipca.g15.campuscarpool.ui.comunidade
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import com.google.gson.JsonObject
import pt.ipca.g15.campuscarpool.di.AppContainer
class HistoricoVm(app: AppContainer) : ComunidadeVm(app) {
    var historico by mutableStateOf<JsonObject?>(null)
    var viagem by mutableStateOf("")
    var pessoas by mutableStateOf(emptyList<Pair<String, String>>())
    fun carregar() {
        pedir({ api.historico() }) {
            historico =
                it
        }
    }
    fun preparar(v: JsonObject, condutor: Boolean) {
        viagem = v.texto("id")
        pessoas = emptyList()
        if (condutor) {
            pedir({ api.passageiros(viagem) }) {
                pessoas =
                    it.map { p -> p.texto("passageiroId") to p.texto("nome") }
            }
        } else {
            pedir({ api.detalhe(viagem) }) {
                pessoas =
                    listOf(it.texto("condutorId") to it.objeto("condutor")?.texto("nome").orEmpty())
            }
        }
    }
    fun avaliar(id: String, pontos: Int, editar: Boolean) {
        if (viagem.isBlank() ||
            pontos !in 1..5 ||
            pessoas.none { it.first == id }
        ) {
            return
        }
        if (editar) {
            acao({
                api.alterarAvaliacao(
                    id,
                    corpo(
                        "classificacao" to pontos
                    )
                )
            }) { aviso = "Avaliação atualizada." }
        } else {
            pedir({
                api.avaliar(
                    corpo(
                        "viagemId" to viagem,
                        "avaliadoId" to id,
                        "classificacao" to pontos
                    )
                )
            }) {
                aviso =
                    "Avaliação guardada."
            }
        }
    }
}

@Composable fun HistoricoScreen() {
    val vm: HistoricoVm = obterVm { HistoricoVm(it) }
    var avaliado by rememberSaveable { mutableStateOf("") }
    var pontos by rememberSaveable { mutableStateOf("5") }
    var editar by rememberSaveable { mutableStateOf(false) }
    LaunchedEffect(Unit) { vm.carregar() }
    Text("Histórico e avaliações", style = MaterialTheme.typography.headlineSmall)
    Estado(vm)
    Botao("Atualizar", !vm.ocupado) { vm.carregar() }
    vm.historico?.let { h ->
        for ((chave, rotulo) in listOf(
            "viagensComoCondutor" to "Viagens como condutor",
            "viagensComoPassageiro" to "Viagens como passageiro"
        )) {
            Text(rotulo)
            h.lista(chave).forEach { v ->
                Bloco {
                    Text("${v.texto("origem")} → ${v.texto("destino")} · ${v.texto("estado")}")
                    Text(dataPt(v.texto("partidaEm")))
                    if (v.texto("estado") ==
                        "Concluida"
                    ) {
                        Botao("Avaliar participante", !vm.ocupado) {
                            avaliado = ""
                            editar = false
                            vm.preparar(
                                v,
                                chave == "viagensComoCondutor"
                            )
                        }
                    }
                }
            }
        }
        for ((chave, rotulo) in listOf(
            "pedidosEfetuados" to "Pedidos efetuados",
            "pedidosRecebidos" to "Pedidos recebidos",
            "notificacoes" to "Notificações",
            "avisos" to "Avisos"
        )) {
            Text(rotulo)
            h.lista(chave).forEach { r ->
                Text(
                    "${r.texto(
                        "origem"
                    )} ${r.texto(
                        "destino"
                    )} ${r.texto(
                        "estado"
                    )} ${r.texto("titulo")} ${r.texto("conteudo")}"
                )
            }
        }
    }
    if (vm.pessoas.isNotEmpty()) {
        Text("Selecionar participante")
        vm.pessoas.forEach { (id, nome) ->
            Botao(
                if (avaliado ==
                    id
                ) {
                    "✓ $nome"
                } else {
                    nome
                }
            ) { avaliado = id }
        }
        Campo("Classificação de 1 a 5", pontos) {
            pontos =
                it
        }
        Botao(if (editar)"Modo: alterar avaliação" else "Modo: nova avaliação") {
            editar =
                !editar
        }
        Text(
            "A avaliação é anónima no perfil público. A API atual não devolve a tua classificação anterior neste ecrã."
        )
        Botao(
            "Guardar",
            !vm.ocupado &&
                avaliado.isNotBlank() &&
                pontos.toIntOrNull() in 1..5
        ) {
            vm.avaliar(avaliado, pontos.toInt(), editar)
        }
    }
}
