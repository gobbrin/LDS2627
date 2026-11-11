package pt.ipca.g15.campuscarpool.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp

/** Coluna com deslocamento, para os formulários continuarem acessíveis com o teclado aberto. */
@Composable fun EcraDeslocavel(conteudo: @Composable ColumnScope.() -> Unit) {
    Column(
        Modifier.verticalScroll(rememberScrollState()).imePadding().padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
        content = conteudo
    )
}

@Composable fun Titulo(texto: String) {
    Text(texto, style = MaterialTheme.typography.headlineSmall)
}

/** Mensagem de erro junto do formulário, anunciada de imediato pelos leitores de ecrã. */
@Composable fun MensagemErro(texto: String?) {
    if (texto != null) {
        Text(
            texto,
            Modifier.semantics { liveRegion = LiveRegionMode.Assertive },
            color = MaterialTheme.colorScheme.error
        )
    }
}

@Composable fun CampoTexto(
    valor: String,
    aoMudar: (String) -> Unit,
    rotulo: String,
    etiqueta: String,
    ativo: Boolean,
    erro: String? = null,
    teclado: KeyboardType = KeyboardType.Text,
    oculto: Boolean = false,
    ajuda: String? = null
) {
    OutlinedTextField(
        valor,
        aoMudar,
        Modifier.fillMaxWidth().testTag(etiqueta),
        label = { Text(rotulo) },
        singleLine = true,
        enabled = ativo,
        isError = erro != null,
        supportingText = (erro ?: ajuda)?.let { texto -> { Text(texto) } },
        visualTransformation =
        if (oculto) PasswordVisualTransformation() else VisualTransformation.None,
        keyboardOptions = KeyboardOptions(keyboardType = teclado)
    )
}
