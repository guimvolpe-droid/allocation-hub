import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../core/api.service';
import { AuditLog } from '../../core/models';

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [MatTableModule, MatIconModule, DatePipe],
  template: `
    <h1>Audit log</h1>
    <p class="lead">Critical admin actions leave a trace: who changed what, when.</p>
    <table mat-table [dataSource]="rows()" class="mat-elevation-z1 full">
      <ng-container matColumnDef="when"><th mat-header-cell *matHeaderCellDef>When</th><td mat-cell *matCellDef="let a">{{ a.createdAt | date:'short' }}</td></ng-container>
      <ng-container matColumnDef="actor"><th mat-header-cell *matHeaderCellDef>Actor</th><td mat-cell *matCellDef="let a">{{ a.actor }}</td></ng-container>
      <ng-container matColumnDef="action"><th mat-header-cell *matHeaderCellDef>Action</th><td mat-cell *matCellDef="let a">{{ a.action }} {{ a.entity }}</td></ng-container>
      <ng-container matColumnDef="details"><th mat-header-cell *matHeaderCellDef>Details</th><td mat-cell *matCellDef="let a" class="mono">{{ a.details }}</td></ng-container>
      <tr mat-header-row *matHeaderRowDef="cols"></tr>
      <tr mat-row *matRowDef="let row; columns: cols;"></tr>
    </table>
    @if (rows().length === 0) { <p class="muted">No activity yet. Change the matching settings to see an entry.</p> }
  `,
  styles: [`
    h1 { margin: 0 0 6px; } .lead { color: #666; margin: 0 0 16px; }
    .full { width: 100%; } .muted { color: #999; margin-top: 12px; }
    .mono { font-family: ui-monospace, monospace; font-size: .82rem; color: #555; }
  `],
})
export class AuditComponent {
  private api = inject(ApiService);
  cols = ['when', 'actor', 'action', 'details'];
  rows = signal<AuditLog[]>([]);
  constructor() { this.api.listAudit().subscribe(r => this.rows.set(r)); }
}
