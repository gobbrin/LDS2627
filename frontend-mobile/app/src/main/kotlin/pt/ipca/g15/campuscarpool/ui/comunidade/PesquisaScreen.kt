package pt.ipca.g15.campuscarpool.ui.comunidade
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.google.gson.JsonObject
import org.osmdroid.config.Configuration
import org.osmdroid.util.BoundingBox
import org.osmdroid.util.GeoPoint
import org.osmdroid.views.MapView
import org.osmdroid.views.overlay.Marker
import pt.ipca.g15.campuscarpool.data.remote.ComunidadeApi
import pt.ipca.g15.campuscarpool.di.AppContainer
class PesquisaVm(
    app: AppContainer? = null,
    api: ComunidadeApi = requireNotNull(app).comunidadeApi
) : ComunidadeVm(app, api) {
    var viagens by mutableStateOf(emptyList<JsonObject>())
    var detalhe by mutableStateOf<JsonObject?>(null)
    var rota by mutableStateOf<JsonObject?>(null)
    var pagina by mutableStateOf(1)
    var total by mutableStateOf(0)
    var erroRota by mutableStateOf("")
    fun procurar(origem: String, destino: String, data: String, hora: String, p: Int = 1) {
        if (ocupado)return
        val filtro = mapOf(
            "origem" to origem.trim(),
            "destino" to destino.trim(),
            "data" to data.trim(),
            "hora" to hora.trim(),
            "pagina" to p.toString(),
            "tamanho" to "20"
        ).filterValues {
            it.isNotBlank()
        }
        pedir({ api.pesquisar(filtro) }) {
            viagens =
                it.lista("viagens")
            pagina = p
            total = it.numero("total")?.toInt() ?: 0
        }
    }
    fun abrir(id: String) {
        detalhe = null
        rota = null
        erroRota = ""
        pedir({ api.detalhe(id) }) {
            detalhe = it
            pedir({ api.rota(id) }) { r ->
                rota =
                    r
            }
        }
    }
}

@Composable fun PesquisaScreen(vm: PesquisaVm = obterVm { PesquisaVm(it) }) {
    val sessaoAtual = vm.app?.sessao?.estado?.collectAsStateWithLifecycle()?.value
    var origem by rememberSaveable { mutableStateOf("") }
    var destino by rememberSaveable { mutableStateOf("") }
    var data by rememberSaveable { mutableStateOf("") }
    var hora by rememberSaveable { mutableStateOf("") }
    Text("Encontrar viagem", style = MaterialTheme.typography.headlineSmall)
    Campo("Origem", origem) {
        origem =
            it
    }
    Campo("Destino", destino) { destino = it }
    Campo("Data (AAAA-MM-DD)", data) { data = it }
    Campo("Hora mínima (HH:mm)", hora) {
        hora =
            it
    }
    Botao("Pesquisar", !vm.ocupado) { vm.procurar(origem, destino, data, hora) }
    Estado(vm)
    Text("${vm.total} viagens encontradas")
    for (v in vm.viagens) {
        Bloco {
            Text("${v.texto("origem")} → ${v.texto("destino")}")
            Text("${dataPt(v.texto("partidaEm"))} · ${v.texto("lugaresDisponiveis")} lugares")
            Text(v.objeto("condutor")?.texto("nome").orEmpty())
            Botao("Consultar detalhe", !vm.ocupado) { vm.abrir(v.texto("id")) }
        }
    }
    Row {
        Botao("Anterior", !vm.ocupado && vm.pagina > 1) {
            vm.procurar(origem, destino, data, hora, vm.pagina - 1)
        }
        Botao(
            "Seguinte",
            !vm.ocupado && vm.pagina * 20 < vm.total
        ) {
            vm.procurar(origem, destino, data, hora, vm.pagina + 1)
        }
    }
    vm.detalhe?.let { v ->
        Bloco {
            Text(
                "${v.texto("origem")} → ${v.texto("destino")}",
                style = MaterialTheme.typography.titleLarge
            )
            Text(
                "${dataPt(
                    v.texto("partidaEm")
                )} · ${v.texto("estado")} · ${v.texto("lugaresDisponiveis")} lugares"
            )
            Text(
                "${v.texto("distanciaKm").ifBlank{
                    "Indisponível"
                }} km · ${v.texto("duracaoMinutos").ifBlank{"Indisponível"}} min"
            )
            Text(
                "Condutor: ${v.objeto(
                    "condutor"
                )?.texto(
                    "nome"
                )} · classificação ${v.objeto("condutor")?.texto("classificacaoMedia")?.ifBlank{
                    "Sem avaliações"
                }}"
            )
            val custo = v.objeto("estimativaCusto")
            Text(
                if (custo !=
                    null
                ) {
                    "Custo indicativo: ${custo.texto(
                        "custoPorParticipante"
                    )} ${custo.texto(
                        "moeda"
                    )} por participante. Total ${custo.texto(
                        "custoTotalEstimado"
                    )}. Não representa um pagamento."
                } else {
                    v.texto("estimativaIndisponivelMotivo").ifBlank { "Estimativa indisponível." }
                }
            )
            vm.rota?.let { MapaPontos(it.lista("pontosOrdenados")) }
            var participacoes by remember(v.texto("id")) { mutableStateOf(false) }
            Botao("Pedir ou gerir participação") {
                participacoes =
                    !participacoes
            }
            if (participacoes)ParticipacoesScreen(v.texto("id"))
            var comunicacao by remember(v.texto("id")) { mutableStateOf("") }
            Botao("Avisos da viagem") {
                comunicacao =
                    "avisos"
            }
            Botao("Contactar condutor") { comunicacao = "mensagens" }
            if (comunicacao ==
                "avisos"
            ) {
                AvisosScreen(v.texto("id"), v.texto("condutorId") == sessaoAtual?.utilizadorId)
            }
            if (comunicacao ==
                "mensagens"
            ) {
                MensagensScreen(v.texto("condutorId"))
            }
            // DETALHE_ACOES
        }
    }
}

@Composable fun MapaPontos(pontos: List<JsonObject>) {
    val coordenadas = pontos.mapNotNull { p ->
        p.objeto("coordenadas")?.let { c ->
            val lat = c.numero("latitude")
            val lon = c.numero("longitude")
            if (lat !=
                null &&
                lon != null &&
                lat.isFinite() &&
                lon.isFinite() &&
                lat in -90.0..90.0 &&
                lon in -180.0..180.0
            ) {
                GeoPoint(lat, lon)
            } else {
                null
            }
        }
    }
    var mapa by remember { mutableStateOf<MapView?>(null) }
    DisposableEffect(Unit) {
        onDispose {
            mapa?.onPause()
            mapa?.onDetach()
        }
    }
    AndroidView(modifier = Modifier.fillMaxWidth().height(300.dp), factory = { context ->
        Configuration.getInstance().userAgentValue =
            context.packageName
        MapView(context).apply {
            setMultiTouchControls(true)
            controller.setZoom(11.0)
            onResume()
            mapa =
                this
        }
    }, update = { m ->
        m.overlays.clear()
        coordenadas.forEachIndexed { i, p ->
            m.overlays.add(
                Marker(m).apply {
                    position =
                        p
                    title = "Ponto ${i + 1}"
                }
            )
        }
        if (coordenadas.isNotEmpty()) {
            m.controller.setCenter(coordenadas.first())
            m.post {
                if (coordenadas.size >
                    1
                ) {
                    m.zoomToBoundingBox(BoundingBox.fromGeoPoints(coordenadas), true, 32)
                }
            }
        }
        m.invalidate()
    })
    Text(
        "© OpenStreetMap contributors. Marcadores dos pontos; o traçado rodoviário ainda não está disponível. Sem rede, consulta a lista."
    )
    pontos.forEachIndexed { i, p -> Text("${i + 1}. ${p.texto("local")} (${p.texto("tipo")})") }
}
