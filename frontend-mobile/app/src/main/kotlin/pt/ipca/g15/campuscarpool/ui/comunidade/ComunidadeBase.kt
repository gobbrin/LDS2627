package pt.ipca.g15.campuscarpool.ui.comunidade
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.google.gson.JsonObject
import kotlinx.coroutines.launch
import pt.ipca.g15.campuscarpool.CampusCarpoolApplication
import pt.ipca.g15.campuscarpool.data.remote.ComunidadeApi
import pt.ipca.g15.campuscarpool.data.remote.ErrosApi
import pt.ipca.g15.campuscarpool.di.AppContainer
import pt.ipca.g15.campuscarpool.domain.model.Resultado
import retrofit2.Response
fun JsonObject.texto(k: String): String = get(k)?.takeUnless {
    it.isJsonNull
}?.let { if (it.isJsonPrimitive)it.asString else "" }.orEmpty()
fun JsonObject.numero(k: String): Double? =
    get(k)?.takeUnless { it.isJsonNull }?.let { runCatching { it.asDouble }.getOrNull() }
fun JsonObject.objeto(k: String): JsonObject? = get(k)?.takeIf { it.isJsonObject }?.asJsonObject
fun JsonObject.lista(k: String): List<JsonObject> = get(k)?.takeIf {
    it.isJsonArray
}?.asJsonArray?.mapNotNull { if (it.isJsonObject)it.asJsonObject else null }.orEmpty()
fun corpo(vararg valores: Pair<String, Any?>): JsonObject = JsonObject().apply {
    valores.forEach { (k, v) ->
        when (v) {
            is Number -> addProperty(k, v)
            is Boolean -> addProperty(k, v)
            else -> addProperty(k, v?.toString())
        }
    }
}
open class ComunidadeVm(
    val app: AppContainer? = null,
    val api: ComunidadeApi = requireNotNull(app).comunidadeApi,
    val utilizadorId: () -> String? = { app?.sessao?.estado?.value?.utilizadorId }
) : ViewModel() {
    var ocupado by mutableStateOf(false)
        private set
    var erro by mutableStateOf("")
        protected set
    var aviso by mutableStateOf("")
        protected set
    fun <T> pedir(chamada: suspend () -> Response<T>, sucesso: (T) -> Unit) {
        if (ocupado)return
        ocupado = true
        erro = ""
        aviso = ""
        viewModelScope.launch {
            val resultado = try {
                ErrosApi.executar(chamada)
            } finally {
                ocupado = false
            }
            when (resultado) {
                is Resultado.Falha -> erro = resultado.mensagem
                is Resultado.Sucesso -> sucesso(resultado.valor)
            }
        }
    }
    fun acao(chamada: suspend () -> Response<Unit>, sucesso: () -> Unit) {
        if (ocupado)return
        ocupado = true
        erro = ""
        aviso = ""
        viewModelScope.launch {
            val resultado = try {
                ErrosApi.executarSemCorpo(chamada)
            } finally {
                ocupado = false
            }
            when (resultado) {
                is Resultado.Falha -> erro = resultado.mensagem
                is Resultado.Sucesso -> sucesso()
            }
        }
    }
    fun falha(texto: String) {
        erro = texto
    }
}

@Composable inline fun <reified T : ComunidadeVm> obterVm(crossinline f: (AppContainer) -> T): T {
    val app = (LocalContext.current.applicationContext as CampusCarpoolApplication).container
    return viewModel(factory = viewModelFactory { initializer { f(app) } })
}

@Composable fun Campo(rotulo: String, valor: String, mudar: (String) -> Unit) {
    OutlinedTextField(value = valor, onValueChange = mudar, label = {
        Text(rotulo)
    }, modifier = Modifier.fillMaxWidth().testTag("comunidade-$rotulo"), singleLine = true)
}

@Composable fun Botao(texto: String, ativo: Boolean = true, acao: () -> Unit) {
    Button(onClick = acao, enabled = ativo) { Text(texto) }
}

@Composable fun Estado(vm: ComunidadeVm) {
    if (vm.ocupado)LinearProgressIndicator(Modifier.fillMaxWidth())
    if (vm.erro.isNotBlank())Text(vm.erro, color = MaterialTheme.colorScheme.error)
    if (vm.aviso.isNotBlank())Text(vm.aviso)
}

@Composable fun Bloco(conteudo: @Composable ColumnScope.() -> Unit) {
    Card(Modifier.fillMaxWidth().padding(vertical = 4.dp)) {
        Column(
            Modifier.padding(12.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
            content = conteudo
        )
    }
}

fun dataPt(valor: String): String = runCatching {
    java.time.Instant.parse(
        valor
    ).atZone(
        java.time.ZoneId.of("Europe/Lisbon")
    ).format(
        java.time.format.DateTimeFormatter.ofPattern(
            "dd/MM/yyyy HH:mm",
            java.util.Locale.forLanguageTag("pt-PT")
        )
    )
}.getOrDefault(valor)
