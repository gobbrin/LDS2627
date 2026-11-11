package pt.ipca.g15.campuscarpool.data.remote
import pt.ipca.g15.campuscarpool.domain.model.AtualizarPerfilPedido
import pt.ipca.g15.campuscarpool.domain.model.ConfirmarPedido
import pt.ipca.g15.campuscarpool.domain.model.Credenciais
import pt.ipca.g15.campuscarpool.domain.model.LigacaoPerfil
import pt.ipca.g15.campuscarpool.domain.model.MensagemResposta
import pt.ipca.g15.campuscarpool.domain.model.NovaLigacaoPedido
import pt.ipca.g15.campuscarpool.domain.model.PerfilAtualizado
import pt.ipca.g15.campuscarpool.domain.model.PerfilProprio
import pt.ipca.g15.campuscarpool.domain.model.PerfilPublico
import pt.ipca.g15.campuscarpool.domain.model.ReenviarConfirmacaoPedido
import pt.ipca.g15.campuscarpool.domain.model.RegistoPedido
import pt.ipca.g15.campuscarpool.domain.model.SessaoAtual
import pt.ipca.g15.campuscarpool.domain.model.TerminarSessaoPedido
import pt.ipca.g15.campuscarpool.domain.model.TokenAcesso
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
interface ApiService {
    @POST("autenticacao/registo")
    suspend fun registar(@Body dados: RegistoPedido): Response<MensagemResposta>

    @POST("autenticacao/confirmar")
    suspend fun confirmar(@Body dados: ConfirmarPedido): Response<Unit>

    @POST("autenticacao/reenviar-confirmacao")
    suspend fun reenviarConfirmacao(
        @Body dados: ReenviarConfirmacaoPedido
    ): Response<MensagemResposta>

    @POST("autenticacao/login")
    suspend fun login(@Body dados: Credenciais): Response<TokenAcesso>

    @GET("autenticacao/me")
    suspend fun sessaoAtual(): Response<SessaoAtual>

    @POST("autenticacao/logout")
    suspend fun logout(@Body pedido: TerminarSessaoPedido): Response<Unit>

    @GET("utilizadores/me")
    suspend fun perfilProprio(): Response<PerfilProprio>

    @PUT("utilizadores/me")
    suspend fun atualizarPerfil(@Body dados: AtualizarPerfilPedido): Response<PerfilAtualizado>

    @POST("utilizadores/me/ligacoes")
    suspend fun adicionarLigacao(@Body dados: NovaLigacaoPedido): Response<LigacaoPerfil>

    @DELETE("utilizadores/me/ligacoes/{ligacaoId}")
    suspend fun removerLigacao(@Path("ligacaoId") ligacaoId: String): Response<Unit>

    @GET("utilizadores/{id}")
    suspend fun perfilPublico(@Path("id") id: String): Response<PerfilPublico>
}
