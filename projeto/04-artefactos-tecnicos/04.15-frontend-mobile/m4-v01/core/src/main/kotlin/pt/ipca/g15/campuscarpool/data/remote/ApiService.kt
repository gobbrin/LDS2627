package pt.ipca.g15.campuscarpool.data.remote
import pt.ipca.g15.campuscarpool.domain.model.Credenciais
import pt.ipca.g15.campuscarpool.domain.model.PerfilProprio
import pt.ipca.g15.campuscarpool.domain.model.SessaoAtual
import pt.ipca.g15.campuscarpool.domain.model.TerminarSessaoPedido
import pt.ipca.g15.campuscarpool.domain.model.TokenAcesso
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
interface ApiService {
    @POST("autenticacao/login")
    suspend fun login(@Body dados: Credenciais): Response<TokenAcesso>

    @GET("autenticacao/me")
    suspend fun sessaoAtual(): Response<SessaoAtual>

    @POST("autenticacao/logout")
    suspend fun logout(@Body pedido: TerminarSessaoPedido): Response<Unit>

    @GET("utilizadores/me")
    suspend fun perfilProprio(): Response<PerfilProprio>
}
