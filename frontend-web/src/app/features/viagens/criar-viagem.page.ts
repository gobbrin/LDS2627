import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { ErroApi } from '../../core/erro-api';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { DadosViagem, ViagemCriada } from '../../shared/models/viagem';
import { formatarRota } from './partida';
import { ViagemApiService } from './viagem-api.service';
import { ViagemFormComponent } from './viagem-form.component';

@Component({
  selector: 'app-criar-viagem-page',
  imports: [ViagemFormComponent],
  template: `<section class="panel">
    <p class="eyebrow">AS TUAS VIAGENS</p>
    <h1>Criar viagem</h1>
    <app-viagem-form
      textoSubmeter="Criar viagem"
      textoEmCurso="A criar a viagem…"
      [aGuardar]="aGuardar()"
      [erro]="erro()"
      (submetido)="criar($event)"
    />
  </section>`,
})
export class CriarViagemPage {
  private readonly api = inject(ViagemApiService);
  private readonly ui = inject(NotificacaoUiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly aGuardar = signal(false);
  readonly erro = signal('');

  criar(dados: DadosViagem) {
    if (this.aGuardar()) return;
    this.ui.limpar();
    this.erro.set('');
    this.aGuardar.set(true);
    this.api
      .criar(dados)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aGuardar.set(false)),
      )
      .subscribe({
        next: (viagem) => {
          this.ui.sucesso(this.mensagemDeSucesso(viagem), { manterAoNavegar: true });
          void this.router.navigate(['/viagens']);
        },
        // RF-F002-17: a mensagem da API explica a falha do Serviço de Mapas e os dados ficam no formulário.
        error: (erro: ErroApi) => this.erro.set(erro.message),
      });
  }

  private mensagemDeSucesso(viagem: ViagemCriada) {
    const rota = formatarRota(viagem.distanciaKm, viagem.duracaoMinutos);
    return rota ? `Viagem criada. Rota: ${rota}.` : 'Viagem criada.';
  }
}
