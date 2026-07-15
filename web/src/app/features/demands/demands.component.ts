import { Component, Inject, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
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
import { Client, Demand, DemandRequest, SENIORITIES } from '../../core/models';

@Component({
  selector: 'app-demands',
  standalone: true,
  imports: [RouterLink, MatTableModule, MatButtonModule, MatIconModule, MatChipsModule],
  template: `
    <div class="head">
      <h1>Demands</h1>
      <button mat-flat-button color="primary" (click)="edit()"><mat-icon>add</mat-icon> New</button>
    </div>
    <table mat-table [dataSource]="rows()" class="mat-elevation-z1 full">
      <ng-container matColumnDef="title"><th mat-header-cell *matHeaderCellDef>Title</th>
        <td mat-cell *matCellDef="let d"><b>{{ d.title }}</b><div class="sub">{{ d.clientName }}</div></td></ng-container>
      <ng-container matColumnDef="seniority"><th mat-header-cell *matHeaderCellDef>Needs</th><td mat-cell *matCellDef="let d">{{ d.requiredSeniority }}</td></ng-container>
      <ng-container matColumnDef="skills"><th mat-header-cell *matHeaderCellDef>Skills</th>
        <td mat-cell *matCellDef="let d"><mat-chip-set>@for (s of d.requiredSkills; track s) { <mat-chip>{{ s }}</mat-chip> }</mat-chip-set></td></ng-container>
      <ng-container matColumnDef="status"><th mat-header-cell *matHeaderCellDef>Status</th>
        <td mat-cell *matCellDef="let d"><span class="pill" [class]="d.status.toLowerCase()">{{ d.status }}</span></td></ng-container>
      <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef></th>
        <td mat-cell *matCellDef="let d" class="actions">
          <a mat-stroked-button color="primary" [routerLink]="['/demands', d.id, 'matches']"><mat-icon>auto_awesome</mat-icon> Matches</a>
          <button mat-icon-button (click)="edit(d)"><mat-icon>edit</mat-icon></button>
          <button mat-icon-button color="warn" (click)="remove(d)"><mat-icon>delete</mat-icon></button>
        </td></ng-container>
      <tr mat-header-row *matHeaderRowDef="cols"></tr>
      <tr mat-row *matRowDef="let row; columns: cols;"></tr>
    </table>
  `,
  styles: [`
    .head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; }
    h1 { margin: 0; } .full { width: 100%; } .sub { color: #888; font-size: .78rem; }
    .actions { text-align: right; white-space: nowrap; }
    .pill { padding: 2px 8px; border-radius: 10px; font-size: .78rem; }
    .pill.open { background: #e3f2fd; color: #1565c0; }
    .pill.allocated { background: #fff3e0; color: #ef6c00; }
    .pill.closed { background: #f0f0f0; color: #777; }
    mat-chip { font-size: .75rem !important; }
  `],
})
export class DemandsComponent {
  private api = inject(ApiService);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);
  cols = ['title', 'seniority', 'skills', 'status', 'actions'];
  rows = signal<Demand[]>([]);

  constructor() { this.load(); }
  private load() { this.api.listDemands().subscribe(r => this.rows.set(r)); }

  edit(d?: Demand) {
    this.api.listClients().subscribe(clients => {
      this.dialog.open(DemandDialog, { width: '520px', data: { demand: d ?? null, clients } })
        .afterClosed().subscribe(saved => {
          if (saved) { this.snack.open('Saved.', 'OK', { duration: 2000 }); this.load(); }
        });
    });
  }
  remove(d: Demand) {
    if (!confirm(`Delete "${d.title}"?`)) return;
    this.api.deleteDemand(d.id).subscribe(() => { this.snack.open('Deleted.', 'OK', { duration: 2000 }); this.load(); });
  }
}

@Component({
  selector: 'app-demand-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  template: `
    <h2 mat-dialog-title>{{ data.demand ? 'Edit' : 'New' }} demand</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="col">
        <mat-form-field appearance="outline"><mat-label>Client</mat-label>
          <mat-select formControlName="clientId">@for (c of data.clients; track c.id) { <mat-option [value]="c.id">{{ c.name }}</mat-option> }</mat-select></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Title</mat-label><input matInput formControlName="title"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Description</mat-label><textarea matInput rows="2" formControlName="description"></textarea></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Required seniority</mat-label>
          <mat-select formControlName="requiredSeniority">@for (s of seniorities; track s) { <mat-option [value]="s">{{ s }}</mat-option> }</mat-select></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Required skills (comma-separated)</mat-label><input matInput formControlName="requiredSkills"></mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: [`.col { display: flex; flex-direction: column; padding-top: 8px; } mat-form-field { width: 100%; }`],
})
export class DemandDialog {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private ref = inject(MatDialogRef<DemandDialog>);
  data = inject<{ demand: Demand | null; clients: Client[] }>(MAT_DIALOG_DATA);
  seniorities = SENIORITIES;

  form = this.fb.nonNullable.group({
    clientId: [this.data.demand?.clientId ?? this.data.clients[0]?.id ?? 0, Validators.required],
    title: [this.data.demand?.title ?? '', Validators.required],
    description: [this.data.demand?.description ?? ''],
    requiredSeniority: [this.data.demand?.requiredSeniority ?? 'Senior', Validators.required],
    requiredSkills: [(this.data.demand?.requiredSkills ?? []).join(', ')],
  });

  save() {
    const v = this.form.getRawValue();
    const req: DemandRequest = {
      clientId: Number(v.clientId), title: v.title, description: v.description,
      requiredSeniority: v.requiredSeniority as any,
      requiredSkills: v.requiredSkills.split(',').map(s => s.trim()).filter(Boolean),
    };
    const obs = this.data.demand ? this.api.updateDemand(this.data.demand.id, req) : this.api.createDemand(req);
    obs.subscribe(() => this.ref.close(true));
  }
}
