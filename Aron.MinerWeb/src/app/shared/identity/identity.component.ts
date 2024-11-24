import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service'; // 假設你有一個 AuthService 來處理驗證
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-identity',
  templateUrl: './identity.component.html',
  styleUrls: ['./identity.component.scss'],
  imports: [CommonModule, RouterLink]
})
export class IdentityComponent
 {
  constructor(public authService: AuthService) { } // 注入 AuthService
}
