import { Component, inject, input, OnInit, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ErroFormularioComponent } from '../../shared/erro-formulario.component';
import { DadosViagem } from '../../shared/models/viagem';
import { diferenteDe } from '../../shared/validadores';
import { paraPartidaUtc, partidaFutura } from './partida';

/** P-11: máximo de lugares oferecidos por viagem. */
const MAXIMO_LUGARES = 8;

type Campo = 'origem' | 'destino' | 'data' | 'hora' | 'lugares';

const MENSAGENS: Record<Campo, Record<string, string>> = {
  origem: {
    required: 'Indica a origem.',
    pattern: 'Indica a origem.',
    maxlength: 'A origem tem no máximo 200 caracteres.',
  },
  destino: {
    required: 'Indica o destino.',
    pattern: 'Indica o destino.',
    maxlength: 'O destino tem no máximo 200 caracteres.',
    diferente: 'A origem e o destino não podem ser o mesmo local.',
  },
  data: { required: 'Indica a data da viagem.' },
  hora: {
    required: 'Indica a hora da viagem.',
    passada: 'A data e a hora têm de ser posteriores ao momento atual.',
  },
  lugares: {
    required: 'Indica o número de lugares.',
    min: 'Indica pelo menos 1 lugar.',
    max: `O máximo é ${MAXIMO_LUGARES} lugares.`,
    pattern: 'Indica um número inteiro de lugares.',
  },
};

export interface ValoresViagem {
  origem: string;
  destino: string;
  data: string;
  hora: string;
  lugares: number;
}

/** Formulário comum à criação e à alteração: as validações são as mesmas (RF-F002-16). */
@Component({
  selector: 'app-viagem-form',
  imports: [ReactiveFormsModule, RouterLink, ErroFormularioComponent],
  template: `<form
    [formGroup]="formulario"
    (ngSubmit)="submeter()"
    [attr.aria-busy]="aGuardar()"
    novalidate
  >
    <label for="origem">Origem</label>
    <input
      id="origem"
      formControlName="origem"
      autocomplete="off"
      [attr.aria-invalid]="invalido('origem')"
      aria-describedby="erro-origem"
    />
    <div id="erro-origem"><app-erro-formulario [erros]="erros('origem')" /></div>
    <label for="destino">Destino</label>
    <input
      id="destino"
      formControlName="destino"
      autocomplete="off"
      [attr.aria-invalid]="invalido('destino')"
      aria-describedby="erro-destino"
    />
    <div id="erro-destino"><app-erro-formulario [erros]="erros('destino')" /></div>
    <div class="campos-partida">
      <div>
        <label for="data">Data</label>
        <input
          id="data"
          type="date"
          formControlName="data"
          [attr.aria-invalid]="invalido('data')"
          aria-describedby="erro-data"
        />
        <div id="erro-data"><app-erro-formulario [erros]="erros('data')" /></div>
      </div>
      <div>
        <label for="hora">Hora</label>
        <input
          id="hora"
          type="time"
          formControlName="hora"
          [attr.aria-invalid]="invalido('hora')"
          aria-describedby="erro-hora"
        />
        <div id="erro-hora"><app-erro-formulario [erros]="erros('hora')" /></div>
      </div>
    </div>
    <label for="lugares">Lugares oferecidos</label>
    <input
      id="lugares"
      type="number"
      inputmode="numeric"
      formControlName="lugares"
      [attr.min]="minimoLugares()"
      [attr.max]="maximoLugares"
      [attr.aria-invalid]="invalido('lugares')"
      aria-describedby="ajuda-lugares erro-lugares"
    />
    <small id="ajuda-lugares" class="muted">{{ ajudaLugares() }}</small>
    <div id="erro-lugares"><app-erro-formulario [erros]="erros('lugares')" /></div>
    @if (erro()) {
      <p class="field-error" role="alert">{{ erro() }}</p>
    }
    <div class="acoes">
      <button class="primary" type="submit" [disabled]="aGuardar()">
        {{ aGuardar() ? textoEmCurso() : textoSubmeter() }}
      </button>
      <a class="button" routerLink="/viagens">Cancelar</a>
    </div>
  </form>`,
})
export class ViagemFormComponent implements OnInit {
  readonly inicial = input<ValoresViagem | null>(null);
  /** Participações aceites: a alteração não pode oferecer menos lugares (RF-F002-08). */
  readonly ocupados = input(0);
  readonly textoSubmeter = input.required<string>();
  readonly textoEmCurso = input.required<string>();
  readonly aGuardar = input(false);
  readonly erro = input('');
  readonly submetido = output<DadosViagem>();
  readonly maximoLugares = MAXIMO_LUGARES;

  // Validação só para usabilidade: quem decide é a API (RF-F002-02, 03, 04 e 16).
  readonly formulario = inject(FormBuilder).nonNullable.group({
    origem: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(200)]],
    destino: [
      '',
      [
        Validators.required,
        Validators.pattern(/\S/),
        Validators.maxLength(200),
        diferenteDe('origem'),
      ],
    ],
    data: ['', Validators.required],
    hora: ['', [Validators.required, partidaFutura('data')]],
    lugares: [
      1 as number | null,
      [
        Validators.required,
        Validators.min(1),
        Validators.max(MAXIMO_LUGARES),
        Validators.pattern(/^\d+$/),
        (controlo: AbstractControl<number | null>) => this.validarOcupados(controlo),
      ],
    ],
  });

  constructor() {
    const { origem, destino, data, hora } = this.formulario.controls;
    origem.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => destino.updateValueAndValidity());
    data.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => hora.updateValueAndValidity());
  }

  ngOnInit() {
    const inicial = this.inicial();
    if (inicial) this.formulario.reset(inicial);
  }

  minimoLugares() {
    return Math.max(1, this.ocupados());
  }

  ajudaLugares() {
    return `De ${this.minimoLugares()} a ${MAXIMO_LUGARES}.`;
  }

  invalido(campo: Campo) {
    const controlo = this.formulario.controls[campo];
    return controlo.touched && controlo.invalid;
  }

  erros(campo: Campo): string[] {
    if (!this.invalido(campo)) return [];
    const [primeiro] = Object.keys(this.formulario.controls[campo].errors ?? {});
    if (primeiro === 'ocupados')
      return [
        `Já tens ${this.ocupados()} lugares ocupados nesta viagem. Não podes oferecer menos.`,
      ];
    return [MENSAGENS[campo][primeiro]];
  }

  submeter() {
    if (this.aGuardar()) return;
    // A hora pode ter passado desde que o campo foi preenchido.
    this.formulario.controls.hora.updateValueAndValidity();
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) return;
    const { origem, destino, data, hora, lugares } = this.formulario.getRawValue();
    this.submetido.emit({
      origem: origem.trim(),
      destino: destino.trim(),
      partidaEm: paraPartidaUtc(data, hora),
      lugares: lugares as number,
    });
  }

  private validarOcupados(controlo: AbstractControl<number | null>) {
    return controlo.value !== null && controlo.value < this.ocupados() ? { ocupados: true } : null;
  }
}
