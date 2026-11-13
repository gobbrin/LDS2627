package pt.ipca.g15.campuscarpool.data.remote
import java.util.concurrent.TimeUnit
import okhttp3.HttpUrl
import okhttp3.HttpUrl.Companion.toHttpUrl
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.Response
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object ApiFactory {
    fun validarUrl(valor: String, desenvolvimento: Boolean = false): HttpUrl {
        val url = valor.toHttpUrl()
        require(
            url.username.isEmpty() &&
                url.password.isEmpty() &&
                url.query == null &&
                url.fragment == null &&
                url.encodedPath == "/api/v1/"
        ) { "Endereço da API inválido." }
        require(
            url.isHttps ||
                (desenvolvimento && url.host in setOf("localhost", "127.0.0.1", "10.0.2.2"))
        ) {
            "Usa HTTPS para o backend."
        }
        return url
    }
    fun criar(url: HttpUrl, sessao: SessaoManager): ApiService {
        val client = OkHttpClient.Builder().connectTimeout(
            15,
            TimeUnit.SECONDS
        ).readTimeout(30, TimeUnit.SECONDS).callTimeout(40, TimeUnit.SECONDS)
            .followRedirects(false).followSslRedirects(false)
            .addInterceptor(
                AuthInterceptor(url, sessao)
            ).addInterceptor(ErroInterceptor(url, sessao)).build()
        return Retrofit.Builder().baseUrl(
            url
        ).client(
            client
        ).addConverterFactory(GsonConverterFactory.create()).build().create(ApiService::class.java)
    }
}
internal fun pertenceApi(url: HttpUrl, base: HttpUrl) = url.scheme == base.scheme &&
    url.host == base.host &&
    url.port == base.port &&
    url.encodedPath.startsWith(base.encodedPath)
class AuthInterceptor(private val base: HttpUrl, private val sessao: SessaoManager) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val pedido = chain.request()
        val publico =
            pedido.url.encodedPath.removePrefix(base.encodedPath) in
                setOf(
                    "autenticacao/login",
                    "autenticacao/registo",
                    "autenticacao/confirmar",
                    "autenticacao/reenviar-confirmacao"
                )
        val token = if (pertenceApi(pedido.url, base) && !publico) sessao.obterToken() else null
        return chain.proceed(
            if (token ==
                null
            ) {
                pedido
            } else {
                pedido.newBuilder().header("Authorization", "Bearer $token").build()
            }
        )
    }
}
class ErroInterceptor(private val base: HttpUrl, private val sessao: SessaoManager) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val pedido = chain.request()
        val resposta = chain.proceed(pedido)
        if (resposta.code == 401 &&
            pertenceApi(pedido.url, base)
        ) {
            sessao.terminarSeToken(pedido.header("Authorization")?.removePrefix("Bearer "))
        }
        return resposta
    }
}
