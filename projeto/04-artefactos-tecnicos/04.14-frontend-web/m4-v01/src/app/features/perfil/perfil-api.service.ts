import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../core/api-client';
import { PerfilProprio } from '../../shared/models/perfil';
@Injectable({ providedIn: 'root' })
export class PerfilApiService {
  private readonly api = inject(ApiClient);
  obterProprio() {
    return this.api.get<PerfilProprio>('utilizadores/me');
  }
}
