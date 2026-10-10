import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  private fb = inject(FormBuilder);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  cargando = false;
  error: string | null = null;

  constructor(private auth: AuthService, private router: Router) {}

  onSubmit(): void {
    if (this.form.invalid) return;
    this.cargando = true;
    this.error = null;

    const { email, password } = this.form.value;
    this.auth.login(email!, password!).subscribe({
      next: () => { this.cargando = false; this.router.navigateByUrl(this.auth.rutaInicio()); },
      error: (err) => {
        this.cargando = false;
        this.error = err.status === 401 ? 'Credenciales inválidas.' : 'Ocurrió un error al iniciar sesión.';
      }
    });
  }
}
