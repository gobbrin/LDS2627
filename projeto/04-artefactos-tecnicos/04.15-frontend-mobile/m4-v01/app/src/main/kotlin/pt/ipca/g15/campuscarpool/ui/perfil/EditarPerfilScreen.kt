package pt.ipca.g15.campuscarpool.ui.perfil

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
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
import pt.ipca.g15.campuscarpool.domain.ValidacaoFormularios
import pt.ipca.g15.campuscarpool.domain.model.LigacaoPerfil
import pt.ipca.g15.campuscarpool.domain.model.PerfilProprio
import pt.ipca.g15.campuscarpool.ui.CampoTexto
import pt.ipca.g15.campuscarpool.ui.EcraDeslocavel
import pt.ipca.g15.campuscarpool.ui.MensagemErro
import pt.ipca.g15.campuscarpool.ui.Titulo

@Composable fun EditarPerfilScreen(
    estado: EstadoPerfil,
    repetir: () -> Unit,
    guardar: (String, String, String) -> Unit,
    adicionarLigacao: (String, String) -> Unit,
    removerLigacao: (String) -> Unit,
    voltar: () -> Unit
) {
    val perfil = estado.perfil
    EcraDeslocavel {
        Titulo(stringResource(R.string.editar_perfil))
        when {
            perfil != null -> {
                FormularioDados(perfil, estado, guardar, voltar)
                SeccaoLigacoes(perfil.ligacoesExternas, estado, adicionarLigacao, removerLigacao)
                // As ligações ficam guardadas de imediato; Cancelar só diz respeito aos dados.
                TextButton(onClick = voltar) { Text(stringResource(R.string.voltar_ao_perfil)) }
            }
            estado.aCarregar -> Text(stringResource(R.string.a_carregar))
            else -> {
                MensagemErro(estado.erro)
                Button(onClick = repetir) { Text(stringResource(R.string.tentar)) }
                TextButton(onClick = voltar) { Text(stringResource(R.string.voltar)) }
            }
        }
    }
}

@Composable private fun FormularioDados(
    perfil: PerfilProprio,
    estado: EstadoPerfil,
    guardar: (String, String, String) -> Unit,
    cancelar: () -> Unit
) {
    var nome by rememberSaveable { mutableStateOf(perfil.nome) }
    var curso by rememberSaveable { mutableStateOf(perfil.curso.orEmpty()) }
    var instituicao by rememberSaveable { mutableStateOf(perfil.instituicao.orEmpty()) }
    val ativo = !estado.aGuardar
    CampoTexto(
        nome,
        { nome = it },
        stringResource(R.string.nome),
        "perfil-nome",
        ativo,
        estado.errosEdicao["nome"]
    )
    CampoTexto(
        curso,
        { curso = it },
        stringResource(R.string.campo_curso),
        "perfil-curso",
        ativo,
        estado.errosEdicao["curso"]
    )
    CampoTexto(
        instituicao,
        { instituicao = it },
        stringResource(R.string.campo_instituicao),
        "perfil-instituicao",
        ativo,
        estado.errosEdicao["instituicao"]
    )
    MensagemErro(estado.erroEdicao)
    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        Button(
            onClick = { guardar(nome, curso, instituicao) },
            enabled = ativo,
            modifier = Modifier.testTag("perfil-guardar")
        ) {
            Text(stringResource(if (ativo) R.string.guardar else R.string.a_guardar))
        }
        TextButton(onClick = cancelar, enabled = ativo) {
            Text(stringResource(R.string.cancelar))
        }
    }
}

@Composable private fun SeccaoLigacoes(
    ligacoes: List<LigacaoPerfil>,
    estado: EstadoPerfil,
    adicionar: (String, String) -> Unit,
    remover: (String) -> Unit
) {
    var url by rememberSaveable { mutableStateOf("") }
    var descricao by rememberSaveable { mutableStateOf("") }
    LaunchedEffect(estado.ligacoesAdicionadas) {
        if (estado.ligacoesAdicionadas > 0) {
            url = ""
            descricao = ""
        }
    }
    val ativo = !estado.aAlterarLigacoes
    Text(stringResource(R.string.ligacoes_externas), style = MaterialTheme.typography.titleMedium)
    if (ligacoes.isEmpty()) Text(stringResource(R.string.sem_ligacoes))
    ligacoes.forEach { ligacao ->
        key(ligacao.id) { LinhaLigacao(ligacao, ativo, remover) }
    }
    MensagemErro(estado.erroLigacoes)
    if (ligacoes.size >= ValidacaoFormularios.MAX_LIGACOES) {
        Text(stringResource(R.string.maximo_ligacoes, ValidacaoFormularios.MAX_LIGACOES))
        return
    }
    CampoTexto(
        url,
        { url = it },
        stringResource(R.string.endereco),
        "ligacao-url",
        ativo,
        estado.errosLigacao["url"],
        KeyboardType.Uri
    )
    CampoTexto(
        descricao,
        { descricao = it },
        stringResource(R.string.descricao_opcional),
        "ligacao-descricao",
        ativo,
        estado.errosLigacao["descricao"]
    )
    Button(
        onClick = { adicionar(url, descricao) },
        enabled = ativo,
        modifier = Modifier.testTag("ligacao-adicionar")
    ) {
        Text(stringResource(R.string.adicionar_ligacao))
    }
}

@Composable
private fun LinhaLigacao(ligacao: LigacaoPerfil, ativo: Boolean, remover: (String) -> Unit) {
    var aConfirmar by rememberSaveable { mutableStateOf(false) }
    Column {
        Text(ligacao.descricao ?: ligacao.url)
        if (aConfirmar) {
            Text(stringResource(R.string.remover_ligacao_pergunta))
            Row {
                TextButton(onClick = { remover(ligacao.id) }, enabled = ativo) {
                    Text(stringResource(R.string.remover))
                }
                TextButton(onClick = { aConfirmar = false }, enabled = ativo) {
                    Text(stringResource(R.string.cancelar))
                }
            }
        } else {
            TextButton(onClick = { aConfirmar = true }, enabled = ativo) {
                Text(stringResource(R.string.remover))
            }
        }
    }
}
