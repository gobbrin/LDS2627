package pt.ipca.g15.campuscarpool

import java.io.IOException
import java.util.Base64
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.flow.MutableStateFlow
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.ResponseBody.Companion.toResponseBody
import pt.ipca.g15.campuscarpool.data.remote.ApiService
import pt.ipca.g15.campuscarpool.data.repository.AutenticacaoRepository
import pt.ipca.g15.campuscarpool.data.repository.PerfilRepository
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
import pt.ipca.g15.campuscarpool.data.session.SessionStore
import pt.ipca.g15.campuscarpool.domain.model.Credenciais
import pt.ipca.g15.campuscarpool.domain.model.PerfilProprio
import pt.ipca.g15.campuscarpool.domain.model.Sessao
import pt.ipca.g15.campuscarpool.domain.model.SessaoAtual
import pt.ipca.g15.campuscarpool.domain.model.TerminarSessaoPedido
import pt.ipca.g15.campuscarpool.domain.model.TokenAcesso
import pt.ipca.g15.campuscarpool.ui.AppViewModel
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

internal val perfilDeTeste = PerfilProprio("A", "Ana", "ana@exemplo.test", null, null, 4.5, 2)

/** ApiService controlado pelo teste, sem rede. */
internal class ApiDeTeste : ApiService {
    var loginResposta: Response<TokenAcesso> = Response.success(TokenAcesso(tokenDeTeste(), 3600))
    var atualResposta: Response<SessaoAtual> = Response.success(SessaoAtual("A"))
    var perfilResposta: Response<PerfilProprio> = Response.success(perfilDeTeste)
    var logoutResposta: Response<Unit> = Response.success<Unit>(204, null)
    var semRedeNoLogout = false
    var pedidosPerfil = 0

    override suspend fun login(dados: Credenciais) = loginResposta
    override suspend fun sessaoAtual() = atualResposta
    override suspend fun perfilProprio(): Response<PerfilProprio> {
        pedidosPerfil++
        return perfilResposta
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
    val api = ApiDeTeste().apply(preparar)
    val store = SessaoEmMemoria(guardada)
    val sessao = SessaoManager(store, BACKEND, scope, agora)
    val online = MutableStateFlow(true)
    val vm = AppViewModel(
        sessao,
        online,
        AutenticacaoRepository(api, sessao),
        PerfilRepository(api)
    )
}
