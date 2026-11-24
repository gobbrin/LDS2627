package pt.ipca.g15.campuscarpool.data.remote
import com.google.gson.JsonObject
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query
import retrofit2.http.QueryMap
interface ComunidadeApi {
    @GET("pesquisa/viagens")
    suspend fun pesquisar(@QueryMap filtro: Map<String, String>): Response<JsonObject>

    @GET("viagens/{id}")
    suspend fun detalhe(@Path("id") id: String): Response<JsonObject>

    @GET("rotas/viagens/{id}")
    suspend fun rota(@Path("id") id: String): Response<JsonObject>

    @GET("historico")
    suspend fun historico(): Response<JsonObject>

    @POST("pedidos")
    suspend fun pedir(@Body dados: JsonObject): Response<JsonObject>

    @GET("viagens/{id}/pedidos")
    suspend fun pendentes(@Path("id") id: String): Response<List<JsonObject>>

    @GET("viagens/{id}/passageiros")
    suspend fun passageiros(@Path("id") id: String): Response<List<JsonObject>>

    @POST("viagens/{id}/simulacao")
    suspend fun simular(@Path("id") id: String, @Body dados: JsonObject): Response<JsonObject>

    @POST("participacoes/{id}/{acao}")
    suspend fun decidir(@Path("id") id: String, @Path("acao") acao: String): Response<Unit>

    @GET("mensagens/conversas")
    suspend fun conversas(): Response<List<JsonObject>>

    @GET("mensagens/conversa/{id}")
    suspend fun mensagens(@Path("id") id: String): Response<List<JsonObject>>

    @POST("mensagens")
    suspend fun mensagem(@Body dados: JsonObject): Response<JsonObject>

    @GET("viagens/{id}/avisos")
    suspend fun avisos(@Path("id") id: String): Response<List<JsonObject>>

    @POST("viagens/{id}/avisos")
    suspend fun aviso(@Path("id") id: String, @Body dados: JsonObject): Response<JsonObject>

    @GET("notificacoes")
    suspend fun notificacoes(
        @Query("pagina") pagina: Int,
        @Query("tamanhoPagina") tamanho: Int = 20
    ): Response<JsonObject>

    @PUT("notificacoes/{id}/ler")
    suspend fun ler(@Path("id") id: String): Response<Unit>

    @PUT("notificacoes/ler-todas")
    suspend fun lerTodas(): Response<JsonObject>

    @POST("dispositivos")
    suspend fun dispositivo(@Body dados: JsonObject): Response<Unit>

    @POST("avaliacoes")
    suspend fun avaliar(@Body dados: JsonObject): Response<JsonObject>

    @PUT("avaliacoes/utilizadores/{id}")
    suspend fun alterarAvaliacao(@Path("id") id: String, @Body dados: JsonObject): Response<Unit>
    // METODOS
}
