package pt.ipca.g15.campuscarpool.ui.viagens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TimeInput
import androidx.compose.material3.rememberDatePickerState
import androidx.compose.material3.rememberTimePickerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.key
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import pt.ipca.g15.campuscarpool.R
import pt.ipca.g15.campuscarpool.domain.DataHoraFormatter
import pt.ipca.g15.campuscarpool.domain.ValidacaoFormularios
import pt.ipca.g15.campuscarpool.domain.model.CamposViagem
import pt.ipca.g15.campuscarpool.domain.model.DetalhesViagem
import pt.ipca.g15.campuscarpool.domain.model.EstadosViagem
import pt.ipca.g15.campuscarpool.ui.CampoTexto
import pt.ipca.g15.campuscarpool.ui.EcraDeslocavel
import pt.ipca.g15.campuscarpool.ui.MensagemErro
import pt.ipca.g15.campuscarpool.ui.Titulo

@Composable
fun CriarViagemScreen(estado: EstadoViagemForm, criar: (CamposViagem) -> Unit, voltar: () -> Unit) {
    EcraDeslocavel {
        Titulo(stringResource(R.string.criar_viagem))
        FormularioViagem(
            CamposViagem(),
            estado,
            stringResource(R.string.criar_viagem),
            stringResource(R.string.a_criar_viagem),
            criar,
            voltar
        )
    }
}

@Composable fun GerirViagemScreen(
    estado: EstadoViagemForm,
    repetir: () -> Unit,
    alterar: (CamposViagem) -> Unit,
    indisponibilizar: () -> Unit,
    voltar: () -> Unit
) {
    val viagem = estado.viagem
    EcraDeslocavel {
        Titulo(stringResource(R.string.gerir_viagem))
        when {
            viagem != null -> {
                ResumoViagem(viagem, estado.oferecidos)
                ZonaAlteracao(estado, alterar, voltar)
                if (viagem.estado == EstadosViagem.ATIVA) {
                    key(estado.versao) { ZonaIndisponibilizar(estado, indisponibilizar) }
                }
                TextButton(onClick = voltar) {
                    Text(stringResource(R.string.voltar_as_viagens))
                }
            }
            estado.erroCarregar != null -> {
                MensagemErro(estado.erroCarregar)
                if (estado.podeRepetir) {
                    Button(onClick = repetir) { Text(stringResource(R.string.tentar)) }
                }
                TextButton(onClick = voltar) { Text(stringResource(R.string.voltar_as_viagens)) }
            }
            else -> Text(stringResource(R.string.a_carregar_viagem))
        }
    }
}

@Composable private fun ResumoViagem(viagem: DetalhesViagem, oferecidos: Int?) {
    Text(stringResource(R.string.estado_valor, rotuloEstado(viagem.estado)))
    Text(stringResource(R.string.percurso_valor, viagem.origem, viagem.destino))
    Text(stringResource(R.string.partida_valor, DataHoraFormatter.formatar(viagem.partidaEm)))
    Text(
        if (oferecidos != null) {
            stringResource(R.string.lugares_de, viagem.lugaresDisponiveis, oferecidos)
        } else {
            stringResource(R.string.lugares_disponiveis, viagem.lugaresDisponiveis)
        }
    )
    val rota = DataHoraFormatter.rota(viagem.distanciaKm, viagem.duracaoMinutos)
    if (rota.isNotEmpty()) Text(stringResource(R.string.rota_valor, rota))
}

@Composable private fun ZonaAlteracao(
    estado: EstadoViagemForm,
    alterar: (CamposViagem) -> Unit,
    cancelar: () -> Unit
) {
    val inicial = estado.camposIniciais()
    if (estado.alteravel() && inicial != null) {
        Text(stringResource(R.string.alterar_dados), style = MaterialTheme.typography.titleMedium)
        // A chave faz o formulário recomeçar dos valores lidos depois de guardar ou de um 409.
        key(estado.versao) {
            FormularioViagem(
                inicial,
                estado,
                stringResource(R.string.guardar_alteracoes),
                stringResource(R.string.a_guardar),
                alterar,
                cancelar
            )
        }
    } else {
        Text(stringResource(R.string.viagem_nao_alteravel))
        MensagemErro(estado.erro)
    }
}

@Composable
private fun ZonaIndisponibilizar(estado: EstadoViagemForm, indisponibilizar: () -> Unit) {
    var aConfirmar by rememberSaveable { mutableStateOf(false) }
    val ativo = !estado.aIndisponibilizar
    Text(
        stringResource(R.string.indisponibilizar_viagem),
        style = MaterialTheme.typography.titleMedium
    )
    if (aConfirmar) {
        Text(stringResource(R.string.indisponibilizar_pergunta))
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            Button(
                onClick = indisponibilizar,
                enabled = ativo,
                modifier = Modifier.testTag("viagem-confirmar-indisponibilizar")
            ) {
                Text(
                    stringResource(
                        if (ativo) R.string.sim_indisponibilizar else R.string.a_indisponibilizar
                    )
                )
            }
            TextButton(onClick = { aConfirmar = false }, enabled = ativo) {
                Text(stringResource(R.string.nao))
            }
        }
    } else {
        Text(stringResource(R.string.indisponibilizar_explicacao))
        TextButton(
            onClick = { aConfirmar = true },
            modifier = Modifier.testTag("viagem-indisponibilizar")
        ) {
            Text(stringResource(R.string.indisponibilizar))
        }
    }
    MensagemErro(estado.erroIndisponibilizar)
}

@Composable private fun FormularioViagem(
    inicial: CamposViagem,
    estado: EstadoViagemForm,
    textoSubmeter: String,
    textoEmCurso: String,
    submeter: (CamposViagem) -> Unit,
    cancelar: () -> Unit
) {
    var origem by rememberSaveable { mutableStateOf(inicial.origem) }
    var destino by rememberSaveable { mutableStateOf(inicial.destino) }
    var data by rememberSaveable { mutableStateOf(inicial.data) }
    var hora by rememberSaveable { mutableStateOf(inicial.hora) }
    var lugares by rememberSaveable { mutableStateOf(inicial.lugares) }
    val erros = estado.errosCampos
    val ativo = !estado.aGuardar && !estado.aIndisponibilizar
    CampoTexto(origem, {
        origem = it
    }, stringResource(R.string.origem), "viagem-origem", ativo, erros["origem"])
    CampoTexto(destino, {
        destino = it
    }, stringResource(R.string.destino), "viagem-destino", ativo, erros["destino"])
    CampoTexto(
        data,
        { data = it },
        stringResource(R.string.data),
        "viagem-data",
        ativo,
        erros["data"],
        ajuda = stringResource(R.string.ajuda_data)
    )
    SeletorData(data, ativo) { data = it }
    CampoTexto(
        hora,
        { hora = it },
        stringResource(R.string.hora),
        "viagem-hora",
        ativo,
        erros["hora"],
        ajuda = stringResource(R.string.ajuda_hora)
    )
    SeletorHora(hora, ativo) { hora = it }
    CampoTexto(
        lugares,
        { lugares = it },
        stringResource(R.string.lugares_oferecidos),
        "viagem-lugares",
        ativo,
        erros["lugares"],
        KeyboardType.Number,
        ajuda = stringResource(
            R.string.ajuda_lugares,
            maxOf(ValidacaoFormularios.MIN_LUGARES, estado.ocupados),
            ValidacaoFormularios.MAX_LUGARES
        )
    )
    MensagemErro(estado.erro)
    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        Button(
            onClick = { submeter(CamposViagem(origem, destino, data, hora, lugares)) },
            enabled = ativo,
            modifier = Modifier.testTag("viagem-guardar")
        ) {
            Text(if (estado.aGuardar) textoEmCurso else textoSubmeter)
        }
        TextButton(onClick = cancelar, enabled = ativo) {
            Text(stringResource(R.string.cancelar))
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun SeletorData(valor: String, ativo: Boolean, escolher: (String) -> Unit) {
    var aberto by rememberSaveable { mutableStateOf(false) }
    TextButton(onClick = { aberto = true }, enabled = ativo) {
        Text(stringResource(R.string.escolher_data))
    }
    if (!aberto) return
    val calendario = rememberDatePickerState(
        initialSelectedDateMillis = DataHoraFormatter.paraCalendario(valor)
    )
    DatePickerDialog(
        onDismissRequest = { aberto = false },
        confirmButton = {
            TextButton(onClick = {
                calendario.selectedDateMillis?.let {
                    escolher(DataHoraFormatter.dataDoCalendario(it))
                }
                aberto = false
            }) { Text(stringResource(R.string.ok)) }
        },
        dismissButton = {
            TextButton(onClick = { aberto = false }) { Text(stringResource(R.string.cancelar)) }
        }
    ) { DatePicker(state = calendario) }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun SeletorHora(valor: String, ativo: Boolean, escolher: (String) -> Unit) {
    var aberto by rememberSaveable { mutableStateOf(false) }
    TextButton(onClick = { aberto = true }, enabled = ativo) {
        Text(stringResource(R.string.escolher_hora))
    }
    if (!aberto) return
    val atual = DataHoraFormatter.lerHora(valor)
    val relogio = rememberTimePickerState(
        initialHour = atual?.hour ?: 0,
        initialMinute = atual?.minute ?: 0,
        is24Hour = true
    )
    AlertDialog(
        onDismissRequest = { aberto = false },
        confirmButton = {
            TextButton(onClick = {
                escolher(DataHoraFormatter.hora(relogio.hour, relogio.minute))
                aberto = false
            }) { Text(stringResource(R.string.ok)) }
        },
        dismissButton = {
            TextButton(onClick = { aberto = false }) { Text(stringResource(R.string.cancelar)) }
        },
        text = { TimeInput(state = relogio) }
    )
}
