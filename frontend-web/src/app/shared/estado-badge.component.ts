import { Component, computed, input } from '@angular/core';

const ROTULOS: Record<string, string> = {
  Ativa: 'Ativa',
  Indisponibilizada: 'Indisponibilizada',
  Concluida: 'Concluída',
};

/** Apresenta um estado devolvido pela API de forma consistente em todos os ecrãs. */
@Component({
  selector: 'app-estado-badge',
  template: `<span class="estado" [class.estado-ativa]="estado() === 'Ativa'">{{
    rotulo()
  }}</span>`,
})
export class EstadoBadgeComponent {
  readonly estado = input.required<string>();
  readonly rotulo = computed(() => ROTULOS[this.estado()] ?? this.estado());
}
