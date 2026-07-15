import { Component, Inject, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api.service';
import { AVAILABILITIES, Consultant, ConsultantRequest, SENIORITIES } from '../../core/models';

@Component({
  selector: 'app-consultants',
  standalone: true,
  imports: [MatTableModule, MatButtonModule, MatIconModule, MatChipsModule],
  template: `
    <div class="head">
      <h1>Consultants</h1>
      <button mat-flat-button color="primary" (click)="edit()"><mat-icon>add</mat-icon> New</button>
    </div>
    <table mat-table [dataSource]="rows()" class="mat-elevation-z1 full">
      <ng-container matColumnDef="name"><th mat-header-cell *matHeaderCellDef>Name</th>
        <td mat-cell *matCellDef="let c"><b>{{ c.name }}</b><div class="sub">{{ c.location }}</div></td></ng-container>
      <ng-container matColumnDef="seniority"><th mat-header-cell *matHeaderCellDef>Seniority</th>
        <td mat-cell *matCellDef="let c">{{ c.seniority }}</td></ng-container>
      <ng-container matColumnDef="availability"><th mat-header-cell *matHeaderCellDef>Availability</th>
        <td mat-cell *matCellDef="let c"><span class="pill" [class]="c.availability.toLowerCase()">{{ c.availability }}</span></td></ng-container>
      <ng-container matColumnDef="rate"><th mat-header-cell *matHeaderCellDef>Rate</th>
        <td mat-cell *matCellDef="let c">\${{ c.hourlyRate }}/h</td></ng-container>
      <ng-container matColumnDef="skills"><th mat-header-cell *matHeaderCellDef>Skills</th>
        <td mat-cell *matCellDef="let c"><mat-chip-set>@for (s of c.skills; track s) { <mat-chip>{{ s }}</mat-chip> }</mat-chip-set></td></ng-container>
      <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef></th>
        <td mat-cell *matCellDef="let c" class="actions">
          <button mat-icon-button (click)="edit(c)"><mat-icon>edit</mat-icon></button>
          <button mat-icon-button color="warn" (click)="remove(c)"><mat-icon>delete</mat-icon></button>
        </td></ng-container>
      <tr mat-header-row *matHeaderRowDef="cols"></tr>
      <tr mat-row *matRowDef="let row; columns: cols;"></tr>
    </table>
  `,
  styles: [`
    .head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; }
    h1 { margin: 0; } .full { width: 100%; } .sub { color: #888; font-size: .78rem; }
    .actions { white-space: nowrap; text-align: right; }
    .pill { padding: 2px 8px; border-radius: 10px; font-size: .78rem; }
    .pill.available { background: #e8f5e9; color: #2e7d32; }
    .pill.allocated { background: #fff3e0; color: #ef6c00; }
    .pill.unavailable { background: #f0f0f0; color: #777; }
    mat-chip { font-size: .75rem !important; }
  `],
})
export class ConsultantsComponent {
  private api = inject(ApiService);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);
  cols = ['name', 'seniority', 'availability', 'rate', 'skills', 'actions'];
  rows = signal<Consultant[]>([]);

  constructor() { this.load(); }
  private load() { this.api.listConsultants().subscribe(r => this.rows.set(r)); }

  edit(c?: Consultant) {
    this.dialog.open(ConsultantDialog, { width: '480px', data: c ?? null }).afterClosed().subscribe(saved => {
      if (saved) { this.snack.open('Saved.', 'OK', { duration: 2000 }); this.load(); }
    });
  }

  remove(c: Consultant) {
    if (!confirm(`Delete ${c.name}?`)) return;
    this.api.deleteConsultant(c.id).subscribe(() => { this.snack.open('Deleted.', 'OK', { duration: 2000 }); this.load(); });
  }
}

@Component({
  selector: 'app-consultant-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Edit' : 'New' }} consultant</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="grid">
        <mat-form-field appearance="outline"><mat-label>Name</mat-label><input matInput formControlName="name"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Email</mat-label><input matInput formControlName="email"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Seniority</mat-label>
          <mat-select formControlName="seniority">@for (s of seniorities; track s) { <mat-option [value]="s">{{ s }}</mat-option> }</mat-select></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Availability</mat-label>
          <mat-select formControlName="availability">@for (a of availabilities; track a) { <mat-option [value]="a">{{ a }}</mat-option> }</mat-select></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Location</mat-label><input matInput formControlName="location"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Hourly rate</mat-label><input matInput type="number" formControlName="hourlyRate"></mat-form-field>
        <mat-form-field appearance="outline" class="span2"><mat-label>Skills (comma-separated)</mat-label><input matInput formControlName="skills"></mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: [`.grid { display: grid; grid-template-columns: 1fr 1fr; gap: 8px 12px; padding-top: 8px; }
            .span2 { grid-column: 1 / 3; } mat-form-field { width: 100%; }`],
})
export class ConsultantDialog {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private ref = inject(MatDialogRef<ConsultantDialog>);
  data = inject<Consultant | null>(MAT_DIALOG_DATA);
  seniorities = SENIORITIES; availabilities = AVAILABILITIES;

  form = this.fb.nonNullable.group({
    name: [this.data?.name ?? '', Validators.required],
    email: [this.data?.email ?? '', [Validators.required, Validators.email]],
    seniority: [this.data?.seniority ?? 'Mid', Validators.required],
    availability: [this.data?.availability ?? 'Available', Validators.required],
    location: [this.data?.location ?? '', Validators.required],
    hourlyRate: [this.data?.hourlyRate ?? 0, [Validators.required, Validators.min(0)]],
    skills: [(this.data?.skills ?? []).join(', ')],
  });

  save() {
    const v = this.form.getRawValue();
    const req: ConsultantRequest = {
      name: v.name, email: v.email, seniority: v.seniority as any, location: v.location,
      availability: v.availability as any, hourlyRate: Number(v.hourlyRate),
      skills: v.skills.split(',').map(s => s.trim()).filter(Boolean),
    };
    const obs = this.data ? this.api.updateConsultant(this.data.id, req) : this.api.createConsultant(req);
    obs.subscribe(() => this.ref.close(true));
  }
}
