package pt.ipca.g15.campuscarpool.ui.comunidade

import androidx.compose.material3.AlertDialog
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import com.google.gson.JsonObject
import pt.ipca.g15.campuscarpool.data.remote.ComunidadeApi
import pt.ipca.g15.campuscarpool.di.AppContainer

class ParticipacoesVm(
    app: AppContainer? = null,
    api: ComunidadeApi = requireNotNull(app).comunidadeApi,
    utilizadorId: () -> String? = { app?.sessao?.estado?.value?.utilizadorId }
) : ComunidadeVm(app, api, utilizadorId) {
    var id: String? = null
    var viagem by mutableStateOf<JsonObject?>(null)
    var meus by mutableStateOf(emptyList<JsonObject>())
    var pendentes by mutableStateOf(emptyList<JsonObject>())
    var aceites by mutableStateOf(emptyList<JsonObject>())
    var simulacao by mutableStateOf<JsonObject?>(null)

    val condutor
        get() = viagem?.texto("condutorId") == this.utilizadorId()

    fun carregar(novoId: String? = id) {
        if (ocupado) return
        id = novoId
        simulacao = null
        pedir({ api.historico() }) {
            meus = it.lista("pedidosEfetuados")
            if (id != null) {
                pedir({ api.detalhe(id!!) }) { v ->
                    viagem = v
                    if (condutor) {
                        pedir({ api.pendentes(id!!) }) { p ->
                            pendentes = p
                            pedir({ api.passageiros(id!!) }) { a -> aceites = a }
                        }
                    }
                }
            }
        }
    }

    fun enviar(recolha: String, destino: String) {
        val v = id ?: return
        pedir({
            api.pedir(
                corpo(
                    "viagemId" to v,
                    "recolha" to recolha.trim().ifBlank { null },
                    "destino" to destino.trim().ifBlank { null }
                )
            )
        }) { carregar() }
    }

    fun simular(pedido: String) {
        val v = id ?: return
        simulacao = null
        pedir({ api.simular(v, corpo("pedidoId" to pedido)) }) { simulacao = it }
    }

    fun decidir(pedido: String, acao: String) {
        if (acao !in setOf("aceitar", "rejeitar", "cancelar")) return
        if (acao == "aceitar" && simulacao?.texto("pedidoId") != pedido) return
        this.acao({ api.decidir(pedido, acao) }) { carregar() }
    }
}

@Composable
fun ParticipacoesScreen(id: String? = null, vm: ParticipacoesVm = obterVm { ParticipacoesVm(it) }) {
    LaunchedEffect(id) {
        vm.carregar(id)
        EventosPedido.alteracoes.collect { vm.carregar(id) }
    }

    var recolha by rememberSaveable { mutableStateOf("") }
    var destino by rememberSaveable { mutableStateOf("") }
    var confirmacao by remember { mutableStateOf<Pair<String, String>?>(null) }

    Text("Participações", style = MaterialTheme.typography.headlineSmall)
    Estado(vm)
    Botao("Atualizar", !vm.ocupado) { vm.carregar(id) }

    vm.viagem?.takeIf { id != null }?.let { v ->
        Text(
            "${v.texto("origem")} → ${v.texto("destino")} · " +
                "${v.texto("lugaresDisponiveis")} lugares"
        )
        if (!vm.condutor) {
            Campo("Recolha (opcional)", recolha) { recolha = it }
            Campo("Destino (opcional)", destino) { destino = it }
            Text(
                "Sem pontos adicionais, usas os extremos da viagem. " +
                    "O pedido aguarda aprovação e não reserva um lugar."
            )
            Botao(
                "Enviar pedido",
                !vm.ocupado &&
                    v.texto("estado") == "Ativa" &&
                    (v.numero("lugaresDisponiveis") ?: 0.0) > 0
            ) {
                vm.enviar(recolha, destino)
            }
        } else {
            Text("Pedidos pendentes")
            vm.pendentes.forEach { p ->
                Bloco {
                    Text("${p.texto("nome")}: ${p.texto("recolha")} → ${p.texto("destino")}")
                    Botao("Simular", !vm.ocupado) { vm.simular(p.texto("id")) }
                    Botao(
                        "Aceitar",
                        !vm.ocupado && vm.simulacao?.texto("pedidoId") == p.texto("id")
                    ) {
                        confirmacao = p.texto("id") to "aceitar"
                    }
                    Botao("Rejeitar", !vm.ocupado) {
                        confirmacao = p.texto("id") to "rejeitar"
                    }
                }
            }
            vm.simulacao?.let { s ->
                Text(
                    "Original: ${s.texto("distanciaOriginalKm")} km / " +
                        "${s.texto("duracaoOriginalMinutos")} min"
                )
                Text(
                    "Simulada: ${s.texto("distanciaSimuladaKm")} km / " +
                        "${s.texto("duracaoSimuladaMinutos")} min"
                )
                Text(
                    "Desvio: ${s.texto("desvioDistanciaKm")} km / " +
                        "${s.texto("desvioDuracaoMinutos")} min"
                )
                Text("Ordem original: ${s.get("ordemOriginal")}")
                Text("Ordem simulada: ${s.get("ordemSimulada")}")
                Text(
                    "A simulação não altera a viagem. " +
                        "O mapa simulado aguarda coordenadas do backend."
                )
            }
            Text("Passageiros aceites")
            vm.aceites.forEach { p ->
                Text(p.texto("nome"))
                Botao("Cancelar participação", !vm.ocupado) {
                    confirmacao = p.texto("id") to "cancelar"
                }
            }
        }
    }

    Text("Os meus pedidos")
    vm.meus.forEach { p ->
        Bloco {
            Text("${p.texto("origem")} → ${p.texto("destino")} · ${p.texto("estado")}")
            if (p.texto("estado") in setOf("Pendente", "Aceite")) {
                Botao("Cancelar", !vm.ocupado) {
                    confirmacao = p.texto("id") to "cancelar"
                }
            }
        }
    }

    confirmacao?.let { (pedido, acao) ->
        AlertDialog(
            onDismissRequest = { confirmacao = null },
            title = { Text("Confirmar decisão") },
            text = { Text("Confirmas que pretendes $acao esta participação?") },
            confirmButton = {
                TextButton(onClick = {
                    confirmacao = null
                    vm.decidir(pedido, acao)
                }) {
                    Text("Confirmar")
                }
            },
            dismissButton = {
                TextButton(onClick = { confirmacao = null }) {
                    Text("Voltar")
                }
            }
        )
    }
}
