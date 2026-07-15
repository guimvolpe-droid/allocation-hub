import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api.service';

const DEFAULTS = { availabilityWeight: 50, skillWeight: 10, seniorityWeight: 20, allocatedPenalty: -30 };

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
  template: `
    <h1>Matching settings</h1>
    <p class="lead">These weights drive the recommendation score. The rule stays deterministic and
      explainable — you're tuning it, not replacing it. Changes are audited.</p>

    <mat-card class="card">
      <form [formGroup]="form" class="grid">
        <mat-form-field appearance="outline">
          <mat-label>Availability bonus</mat-label>
          <input matInput type="number" formControlName="availabilityWeight">
          <mat-hint>Points when the consultant is available now.</mat-hint>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Skill points (each)</mat-label>
          <input matInput type="number" formControlName="skillWeight">
          <mat-hint>Points per required skill the consultant has.</mat-hint>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Seniority bonus</mat-label>
          <input matInput type="number" formControlName="seniorityWeight">
          <mat-hint>Points when seniority meets/exceeds the requirement.</mat-hint>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Allocated penalty</mat-label>
          <input matInput type="number" formControlName="allocatedPenalty">
          <mat-hint>Penalty when the consultant is already allocated (negative).</mat-hint>
        </mat-form-field>
      </form>

      @if (updated()) { <div class="meta">Last updated {{ updated() }} by {{ updatedBy() }}</div> }

      <div class="actions">
        <button mat-stroked-button (click)="restore()"><mat-icon>restart_alt</mat-icon> Restore defaults</button>
        <button mat-flat-button color="primary" [disabled]="form.invalid || saving()" (click)="save()">
          <mat-icon>save</mat-icon> Save
        </button>
      </div>
    </mat-card>
  `,
  styles: [`
    h1 { margin: 0 0 6px; } .lead { color: #666; max-width: 620px; margin: 0 0 18px; }
    .card { padding: 20px; max-width: 680px; }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 8px 16px; }
    mat-form-field { width: 100%; }
    .meta { color: #888; font-size: .82rem; margin: 4px 0 14px; }
    .actions { display: flex; justify-content: flex-end; gap: 10px; }
  `],
})
export class SettingsComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private snack = inject(MatSnackBar);

  saving = signal(false);
  updated = signal<string | null>(null);
  updatedBy = signal<string>('');

  form = this.fb.nonNullable.group({
    availabilityWeight: [DEFAULTS.availabilityWeight, Validators.required],
    skillWeight: [DEFAULTS.skillWeight, Validators.required],
    seniorityWeight: [DEFAULTS.seniorityWeight, Validators.required],
    allocatedPenalty: [DEFAULTS.allocatedPenalty, Validators.required],
  });

  constructor() {
    this.api.getMatchingSettings().subscribe(s => {
      this.form.patchValue(s);
      this.updated.set(new Date(s.updatedAt).toLocaleString());
      this.updatedBy.set(s.updatedBy);
    });
  }

  restore() { this.form.patchValue(DEFAULTS); }

  save() {
    this.saving.set(true);
    this.api.updateMatchingSettings(this.form.getRawValue()).subscribe({
      next: s => {
        this.snack.open('Matching weights saved.', 'OK', { duration: 2500 });
        this.updated.set(new Date(s.updatedAt).toLocaleString());
        this.updatedBy.set(s.updatedBy);
        this.saving.set(false);
      },
      error: () => { this.snack.open('Could not save.', 'OK', { duration: 2500 }); this.saving.set(false); },
    });
  }
}
