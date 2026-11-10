package pt.ipca.g15.campuscarpool

import java.io.IOException
import java.time.Instant
import java.util.Base64
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.flow.MutableStateFlow
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.ResponseBody.Companion.toResponseBody
import pt.ipca.g15.campuscarpool.data.remote.ApiService
import pt.ipca.g15.campuscarpool.data.repository.AutenticacaoRepository
import pt.ipca.g15.campuscarpool.data.repository.PerfilRepository
import pt.ipca.g15.campuscarpool.data.repository.ViagemRepository
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
import pt.ipca.g15.campuscarpool.data.session.SessionStore
import pt.ipca.g15.campuscarpool.domain.model.AtualizarPerfilPedido
import pt.ipca.g15.campuscarpool.domain.model.ConfirmarPedido
import pt.ipca.g15.campuscarpool.domain.model.Credenciais
import pt.ipca.g15.campuscarpool.domain.model.DadosViagem
import pt.ipca.g15.campuscarpool.domain.model.DetalhesViagem
import pt.ipca.g15.campuscarpool.domain.model.LigacaoPerfil
import pt.ipca.g15.campuscarpool.domain.model.MensagemResposta
import pt.ipca.g15.campuscarpool.domain.model.NovaLigacaoPedido
import pt.ipca.g15.campuscarpool.domain.model.PerfilAtualizado
import pt.ipca.g15.campuscarpool.domain.model.PerfilProprio
import pt.ipca.g15.campuscarpool.domain.model.PerfilPublico
import pt.ipca.g15.campuscarpool.domain.model.ReenviarConfirmacaoPedido
import pt.ipca.g15.campuscarpool.domain.model.RegistoPedido
import pt.ipca.g15.campuscarpool.domain.model.Sessao
import pt.ipca.g15.campuscarpool.domain.model.SessaoAtual
import pt.ipca.g15.campuscarpool.domain.model.TerminarSessaoPedido
import pt.ipca.g15.campuscarpool.domain.model.TokenAcesso
import pt.ipca.g15.campuscarpool.domain.model.ViagemCriada
import pt.ipca.g15.campuscarpool.domain.model.ViagemDoCondutor
import pt.ipca.g15.campuscarpool.ui.FabricaViewModels
import pt.ipca.g15.campuscarpool.ui.autenticacao.LoginViewModel
import pt.ipca.g15.campuscarpool.ui.autenticacao.RegistoViewModel
import pt.ipca.g15.campuscarpool.ui.perfil.PerfilViewModel
import pt.ipca.g15.campuscarpool.ui.viagens.MinhasViagensViewModel
import pt.ipca.g15.campuscarpool.ui.viagens.ViagemFormViewModel
import retrofit2.Response

internal const val BACKEND = "https://exemplo.test/api/v1/"

/** Token sintético, sem assinatura válida: a validade real é decidida pelo backend. */
internal fun tokenDeTeste(id: String = "A", exp: Long = 3600): String {
    val corpo = "{\"sub\":\"$id\",\"exp\":$exp}".toByteArray()
    return "header." + Base64.getUrlEncoder().withoutPadding().encodeToString(corpo) + ".assinatura"
}

internal class SessaoEmMemoria(var valor: Sessao? = null) : SessionStore {
    override fun ler() = valor
    override fun guardar(sessao: Sessao) {
        valor = sessao
    }
    override fun limpar() {
        valor = null
    }
}

internal fun <T> erroDaApi(status: Int, mensagem: String = "Pedido recusado"): Response<T> =
    Response.error(
        status,
        "{\"erro\":\"$mensagem\"}".toResponseBody("application/json".toMediaType())
    )

internal val ligacaoDeTeste = LigacaoPerfil("L1", "https://exemplo.test/ana", "Portefólio")
internal val perfilDeTeste = PerfilProprio(
    "A",
    "Ana",
    "ana@exemplo.test",
    null,
    null,
    4.5,
    2,
    listOf(ligacaoDeTeste)
)
internal val perfilPublicoDeTeste =
    PerfilPublico("B", "Rui", "LEI", "IPCA", listOf(ligacaoDeTeste), null, 0)

internal const val ID_VIAGEM = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
internal val viagemCriadaDeTeste =
    ViagemCriada(ID_VIAGEM, "Barcelos", "Braga", "2099-12-01T09:30:00Z", 3, 21.5, 25)
internal val viagemDoCondutorDeTeste =
    ViagemDoCondutor(ID_VIAGEM, "Barcelos", "Braga", "2099-12-01T09:30:00Z", 3, 2)
internal val detalhesViagemDeTeste = DetalhesViagem(
    ID_VIAGEM,
    "Barcelos",
    "Braga",
    "2099-12-01T09:30:00Z",
    2,
    "Ativa",
    21.5,
    25,
    "A"
)

/** ApiService controlado pelo teste, sem rede. */
internal class ApiDeTeste : ApiService {
    var registoResposta: Response<MensagemResposta> =
        Response.success(202, MensagemResposta("Verifica o email."))
    var confirmarResposta: Response<Unit> = Response.success<Unit>(204, null)
    var reenvioResposta: Response<MensagemResposta> =
        Response.success(202, MensagemResposta("Se existir, foi enviada."))
    var loginResposta: Response<TokenAcesso> = Response.success(TokenAcesso(tokenDeTeste(), 3600))
    var atualResposta: Response<SessaoAtual> = Response.success(SessaoAtual("A"))
    var perfilResposta: Response<PerfilProprio> = Response.success(perfilDeTeste)
    var atualizarResposta: Response<PerfilAtualizado>? = null
    var novaLigacaoResposta: Response<LigacaoPerfil> =
        Response.success(201, LigacaoPerfil("L2", "https://exemplo.test/novo", null))
    var removerResposta: Response<Unit> = Response.success<Unit>(204, null)
    var publicoResposta: Response<PerfilPublico> = Response.success(perfilPublicoDeTeste)
    var logoutResposta: Response<Unit> = Response.success<Unit>(204, null)
    var criarViagemResposta: Response<ViagemCriada> = Response.success(201, viagemCriadaDeTeste)
    var minhasViagensResposta: Response<List<ViagemDoCondutor>> =
        Response.success(listOf(viagemDoCondutorDeTeste))
    var viagemResposta: Response<DetalhesViagem> = Response.success(detalhesViagemDeTeste)
    var alterarViagemResposta: Response<Unit> = Response.success<Unit>(204, null)
    var indisponibilizarResposta: Response<Unit> = Response.success<Unit>(204, null)
    var dadosViagemEnviados: DadosViagem? = null
    var pedidosCriarViagem = 0
    var pedidosMinhasViagens = 0
    var pedidosViagem = 0
    var pedidosAlterarViagem = 0
    var pedidosIndisponibilizar = 0
    var semRedeNoLogout = false
    var semRedeNaConfirmacao = false
    var pedidosPerfil = 0
    var pedidosSessao = 0
    var pedidosConfirmar = 0
    var pedidosRegisto = 0
    var pedidosReenvio = 0
    var pedidosAtualizar = 0
    var pedidosLigacao = 0

    override suspend fun registar(dados: RegistoPedido): Response<MensagemResposta> {
        pedidosRegisto++
        return registoResposta
    }
    override suspend fun confirmar(dados: ConfirmarPedido): Response<Unit> {
        pedidosConfirmar++
        if (semRedeNaConfirmacao) throw IOException()
        return confirmarResposta
    }
    override suspend fun reenviarConfirmacao(
        dados: ReenviarConfirmacaoPedido
    ): Response<MensagemResposta> {
        pedidosReenvio++
        return reenvioResposta
    }
    override suspend fun login(dados: Credenciais) = loginResposta
    override suspend fun sessaoAtual(): Response<SessaoAtual> {
        pedidosSessao++
        return atualResposta
    }
    override suspend fun perfilProprio(): Response<PerfilProprio> {
        pedidosPerfil++
        return perfilResposta
    }
    override suspend fun atualizarPerfil(dados: AtualizarPerfilPedido): Response<PerfilAtualizado> {
        pedidosAtualizar++
        return atualizarResposta ?: Response.success(
            PerfilAtualizado("A", dados.nome, "ana@exemplo.test", dados.curso, dados.instituicao)
        )
    }
    override suspend fun adicionarLigacao(dados: NovaLigacaoPedido): Response<LigacaoPerfil> {
        pedidosLigacao++
        return novaLigacaoResposta
    }
    override suspend fun removerLigacao(ligacaoId: String) = removerResposta
    override suspend fun perfilPublico(id: String) = publicoResposta
    override suspend fun criarViagem(dados: DadosViagem): Response<ViagemCriada> {
        pedidosCriarViagem++
        dadosViagemEnviados = dados
        return criarViagemResposta
    }
    override suspend fun minhasViagens(): Response<List<ViagemDoCondutor>> {
        pedidosMinhasViagens++
        return minhasViagensResposta
    }
    override suspend fun viagem(id: String): Response<DetalhesViagem> {
        pedidosViagem++
        return viagemResposta
    }
    override suspend fun alterarViagem(id: String, dados: DadosViagem): Response<Unit> {
        pedidosAlterarViagem++
        dadosViagemEnviados = dados
        return alterarViagemResposta
    }
    override suspend fun indisponibilizarViagem(id: String): Response<Unit> {
        pedidosIndisponibilizar++
        return indisponibilizarResposta
    }
    override suspend fun logout(pedido: TerminarSessaoPedido): Response<Unit> {
        if (semRedeNoLogout) throw IOException()
        return logoutResposta
    }
}

internal class Cenario(
    scope: CoroutineScope,
    guardada: Sessao? = null,
    preparar: ApiDeTeste.() -> Unit = {},
    agora: () -> Long = { 0 }
) {
    var utilizador: String? = "A"
    private val instante = Instant.parse("2026-11-10T10:00:00Z")
    val api = ApiDeTeste().apply(preparar)
    val store = SessaoEmMemoria(guardada)
    val sessao = SessaoManager(store, BACKEND, scope, agora)
    val online = MutableStateFlow(true)
    private val autenticacao = AutenticacaoRepository(api, sessao)
    private val perfis = PerfilRepository(api)
    private val viagens = ViagemRepository(api)
    val fabrica = FabricaViewModels(sessao, autenticacao, perfis, viagens)
    val login = LoginViewModel(sessao, autenticacao)
    val registo by lazy { RegistoViewModel(autenticacao) }
    val perfil by lazy { PerfilViewModel(perfis) }
    val minhasViagens by lazy { MinhasViagensViewModel(viagens) }
    val viagemForm by lazy { ViagemFormViewModel(viagens, { utilizador }, { instante }) }
}
