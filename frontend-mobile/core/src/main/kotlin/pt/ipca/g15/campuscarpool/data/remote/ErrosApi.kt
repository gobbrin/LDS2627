package pt.ipca.g15.campuscarpool.data.remote
import com.google.gson.JsonParser
import java.io.IOException
import kotlinx.coroutines.CancellationException
import pt.ipca.g15.campuscarpool.domain.model.Resultado
import retrofit2.Response

object ErrosApi {
    fun resposta(status: Int, corpo: String?): Resultado.Falha {
        val alternativa = when (status) {
            400 -> "Confirma os dados introduzidos e tenta novamente."
            401 -> "A sessão não é válida. Inicia sessão novamente."
            403 -> "Não tens autorização. Confirma se a conta está confirmada."
            404 -> "A informação pedida não está disponível."
            409 -> "A operação entra em conflito com o estado atual. Atualiza e tenta novamente."
            429 -> "Demasiados pedidos. Aguarda um momento e tenta novamente."
            503 -> "O serviço está temporariamente indisponível. Tenta novamente mais tarde."
            else -> "Não foi possível concluir a operação. Tenta novamente mais tarde."
        }
        // A API tem mensagens próprias nos 4xx e nas falhas de serviços externos (502, 503, 504).
        // Os restantes 5xx ficam com o texto genérico, para nunca exporem detalhes internos.
        if (status !in 400..499 && status !in setOf(502, 503, 504)) {
            return Resultado.Falha(alternativa, status)
        }
        return runCatching {
            val json = JsonParser.parseString(corpo).asJsonObject
            val erro = json.get("erro")
            val texto = if (erro?.isJsonPrimitive == true &&
                erro.asJsonPrimitive.isString
            ) {
                erro.asString
            } else if (erro?.isJsonObject ==
                true
            ) {
                erro.asJsonObject.get("mensagem")?.asString
            } else {
                json.get("detail")?.asString
            }
            val camposJson = if (erro?.isJsonObject ==
                true
            ) {
                erro.asJsonObject.getAsJsonObject("campos")
            } else {
                json.getAsJsonObject("errors")
            }
            val campos = camposJson?.entrySet()?.associate { (nome, valor) ->
                nome to
                    (
                        if (valor.isJsonArray) {
                            valor.asJsonArray.map {
                                it.asString.take(500)
                            }
                        } else {
                            listOf(valor.asString.take(500))
                        }
                        )
            }.orEmpty()
            Resultado.Falha(
                texto?.takeIf {
                    it.isNotBlank()
                }?.take(500) ?: alternativa,
                status,
                campos,
                daApi = !texto.isNullOrBlank()
            )
        }.getOrElse { Resultado.Falha(alternativa, status) }
    }
    suspend fun <T> executar(pedido: suspend () -> Response<T>): Resultado<T> = tentar(pedido) {
        it.body()?.let { corpo -> Resultado.Sucesso(corpo) }
            ?: Resultado.Falha("O servidor devolveu uma resposta incompleta.")
    }

    /** Para respostas sem corpo (202, 204): o Retrofit devolve `body()` nulo num 204. */
    suspend fun executarSemCorpo(pedido: suspend () -> Response<Unit>): Resultado<Unit> =
        tentar(pedido) { Resultado.Sucesso(Unit) }

    private suspend fun <T, R> tentar(
        pedido: suspend () -> Response<T>,
        sucesso: (Response<T>) -> Resultado<R>
    ): Resultado<R> = try {
        val resposta = pedido()
        if (!resposta.isSuccessful) {
            resposta(resposta.code(), resposta.errorBody()?.use { it.string() })
        } else {
            sucesso(resposta)
        }
    } catch (e: CancellationException) {
        throw e
    } catch (
        _: IOException
    ) {
        Resultado.Falha(
            "Não foi possível contactar o servidor. Verifica a ligação e tenta novamente."
        )
    } catch (
        _: Exception
    ) {
        Resultado.Falha("Não foi possível interpretar a resposta. Tenta novamente.")
    }
}
