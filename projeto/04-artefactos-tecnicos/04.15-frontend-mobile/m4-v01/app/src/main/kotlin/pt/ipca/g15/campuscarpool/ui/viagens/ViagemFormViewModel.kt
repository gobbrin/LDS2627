package pt.ipca.g15.campuscarpool.ui.viagens

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import java.time.Instant
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import pt.ipca.g15.campuscarpool.data.repository.ViagemRepository
import pt.ipca.g15.campuscarpool.domain.DataHoraFormatter
import pt.ipca.g15.campuscarpool.domain.ValidacaoFormularios
import pt.ipca.g15.campuscarpool.domain.model.CamposViagem
import pt.ipca.g15.campuscarpool.domain.model.DadosViagem
import pt.ipca.g15.campuscarpool.domain.model.DetalhesViagem
import pt.ipca.g15.campuscarpool.domain.model.EstadosViagem
import pt.ipca.g15.campuscarpool.domain.model.Resultado

data class EstadoViagemForm(
    val aCarregar: Boolean = false,
    val viagem: DetalhesViagem? = null,
    /** `GET /viagens/{id}` não os traz; vêm da lista das viagens ativas. */
    val oferecidos: Int? = null,
    val erroCarregar: String? = null,
    val podeRepetir: Boolean = false,
    /** Aumenta a cada leitura, para o formulário voltar aos valores do servidor. */
    val versao: Int = 0,
    val aGuardar: Boolean = false,
    val errosCampos: Map<String, String> = emptyMap(),
    val erro: String? = null,
    val aIndisponibilizar: Boolean = false,
    val erroIndisponibilizar: String? = null,
    val aviso: String? = null,
    val criada: Boolean = false
) {
    val ocupados: Int
        get() = if (oferecidos == null || viagem == null) {
            0
        } else {
            oferecidos - viagem.lugaresDisponiveis
        }

    /** Só uma viagem ativa e ainda por partir é alterável (RF-F002-07, RF-F002-11). */
    fun alteravel(agora: Instant = Instant.now()): Boolean {
        val partida = viagem?.let { DataHoraFormatter.instanteDe(it.partidaEm) }
        return viagem?.estado == EstadosViagem.ATIVA &&
            oferecidos != null &&
            partida != null &&
            partida.isAfter(agora)
    }

    fun camposIniciais(): CamposViagem? {
        val partida = viagem?.let { DataHoraFormatter.campos(it.partidaEm) } ?: return null
        return CamposViagem(
            viagem.origem,
            viagem.destino,
            partida.data,
            partida.hora,
            oferecidos?.toString().orEmpty()
        )
    }
}

/** Criação e gestão partilham as validações (RF-F002-16) e têm o âmbito de cada ecrã. */
class ViagemFormViewModel(
    private val viagens: ViagemRepository,
    private val utilizadorId: () -> String?,
    private val agora: () -> Instant = Instant::now
) : ViewModel() {
    private val atual = MutableStateFlow(EstadoViagemForm())
    val estado = atual.asStateFlow()
    private var idAberto: String? = null
    private var leitura: Job? = null

    /** Só a primeira chamada lê: rodar o ecrã não volta a pedir nem apaga o que foi escrito. */
    fun abrir(id: String) {
        if (idAberto == id) return
        idAberto = id
        carregar(id)
    }

    fun carregar(id: String) {
        leitura?.cancel()
        atual.update {
            it.copy(
                aCarregar = true,
                viagem = null,
                oferecidos = null,
                erroCarregar = null,
                podeRepetir = false
            )
        }
        leitura = viewModelScope.launch {
            val detalhe = viagens.obterDetalhe(id)
            if (detalhe is Resultado.Falha) return@launch falhaAoLer(detalhe)
            val viagem = (detalhe as Resultado.Sucesso).valor
            val minhas = viagens.listarMinhas()
            if (minhas is Resultado.Falha) return@launch falhaAoLer(minhas)
            // O backend volta a confirmar em cada operação; isto só evita ações inúteis.
            if (!viagem.condutorId.equals(utilizadorId(), ignoreCase = true)) {
                return@launch semRepeticao("Só o condutor pode gerir esta viagem.")
            }
            val oferecidos =
                (minhas as Resultado.Sucesso).valor.find { it.id == viagem.id }?.lugaresOferecidos
            atual.update {
                it.copy(
                    aCarregar = false,
                    viagem = viagem,
                    oferecidos = oferecidos,
                    versao = it.versao + 1
                )
            }
        }
    }

    fun criar(campos: CamposViagem) {
        if (atual.value.aGuardar) return
        val dados = validar(campos, ocupados = 0) ?: return
        atual.update { it.copy(aGuardar = true) }
        viewModelScope.launch {
            val r = viagens.criar(dados)
            atual.update {
                when (r) {
                    is Resultado.Sucesso -> {
                        val rota = DataHoraFormatter.rota(
                            r.valor.distanciaKm,
                            r.valor.duracaoMinutos
                        )
                        it.copy(
                            aGuardar = false,
                            criada = true,
                            aviso = "Viagem criada." + if (rota.isEmpty()) "" else " Rota: $rota."
                        )
                    }
                    // RF-F002-17: a mensagem explica a falha e os dados ficam no formulário.
                    is Resultado.Falha -> it.copy(aGuardar = false, erro = r.mensagem)
                }
            }
        }
    }

    fun alterar(id: String, campos: CamposViagem) {
        if (ocupado()) return
        val dados = validar(campos, atual.value.ocupados) ?: return
        atual.update { it.copy(aGuardar = true) }
        viewModelScope.launch {
            when (val r = viagens.alterar(id, dados)) {
                is Resultado.Sucesso -> {
                    atual.update { it.copy(aGuardar = false, aviso = "Viagem atualizada.") }
                    carregar(id)
                }
                is Resultado.Falha -> {
                    atual.update { it.copy(aGuardar = false, erro = r.mensagem) }
                    // A viagem mudou entretanto (noutro dispositivo ou por um passageiro aceite).
                    if (r.status == CONFLITO) carregar(id)
                }
            }
        }
    }

    fun indisponibilizar(id: String) {
        if (ocupado()) return
        atual.update {
            it.copy(aIndisponibilizar = true, erro = null, erroIndisponibilizar = null)
        }
        viewModelScope.launch {
            when (val r = viagens.indisponibilizar(id)) {
                is Resultado.Sucesso -> {
                    atual.update {
                        it.copy(aIndisponibilizar = false, aviso = "Viagem indisponibilizada.")
                    }
                    carregar(id)
                }
                is Resultado.Falha -> if (r.status == CONFLITO) {
                    // Já não está ativa: a mensagem fica junto do estado atualizado.
                    atual.update { it.copy(aIndisponibilizar = false, erro = r.mensagem) }
                    carregar(id)
                } else {
                    atual.update {
                        it.copy(aIndisponibilizar = false, erroIndisponibilizar = r.mensagem)
                    }
                }
            }
        }
    }

    fun consumirAviso() = atual.update { it.copy(aviso = null) }

    fun consumirCriada() = atual.update { it.copy(criada = false) }

    private fun ocupado() = atual.value.aGuardar || atual.value.aIndisponibilizar

    private fun validar(campos: CamposViagem, ocupados: Int): DadosViagem? {
        val erros = ValidacaoFormularios.viagem(campos, ocupados, agora())
        atual.update { it.copy(errosCampos = erros, erro = null, erroIndisponibilizar = null) }
        if (erros.isNotEmpty()) return null
        val partida = DataHoraFormatter.paraUtc(campos.data, campos.hora) ?: return null
        return DadosViagem(
            campos.origem.trim(),
            campos.destino.trim(),
            partida,
            campos.lugares.trim().toInt()
        )
    }

    private fun falhaAoLer(falha: Resultado.Falha) = if (falha.status == NAO_ENCONTRADO) {
        semRepeticao("Viagem não encontrada.")
    } else {
        atual.update {
            it.copy(aCarregar = false, erroCarregar = falha.mensagem, podeRepetir = true)
        }
    }

    private fun semRepeticao(mensagem: String) = atual.update {
        it.copy(aCarregar = false, erroCarregar = mensagem, podeRepetir = false)
    }

    private companion object {
        const val NAO_ENCONTRADO = 404
        const val CONFLITO = 409
    }
}
