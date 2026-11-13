import { Component, input } from '@angular/core';
import { PerfilPublico } from '../../shared/models/perfil';

/** Campos de P-10, comuns ao perfil próprio e ao de outro utilizador. */
@Component({
  selector: 'app-dados-perfil',
  template: `@let p = perfil();
    <dl>
      <div>
        <dt>Curso</dt>
        <dd>{{ p.curso || 'Não indicado' }}</dd>
      </div>
      <div>
        <dt>Instituição</dt>
        <dd>{{ p.instituicao || 'Não indicada' }}</dd>
      </div>
      <div>
        <dt>Ligações externas</dt>
        <dd>
          @if (p.ligacoesExternas.length) {
            <ul class="ligacoes">
              @for (ligacao of p.ligacoesExternas; track ligacao.id) {
                <li>
                  <a [href]="ligacao.url" target="_blank" rel="noopener noreferrer">{{
                    ligacao.descricao || ligacao.url
                  }}</a>
                </li>
              }
            </ul>
          } @else {
            Sem ligações externas
          }
        </dd>
      </div>
      <div>
        <dt>Avaliações</dt>
        <dd>{{ classificacao(p.classificacaoMedia) }}</dd>
      </div>
    </dl>`,
})
export class DadosPerfilComponent {
  readonly perfil = input.required<PerfilPublico>();

  // RF-F007-10: média com 1 casa decimal.
  classificacao(media: number | null) {
    if (media === null) return 'Sem avaliações';
    const texto = media.toLocaleString('pt-PT', {
      minimumFractionDigits: 1,
      maximumFractionDigits: 1,
    });
    return `${texto} / 5`;
  }
}
