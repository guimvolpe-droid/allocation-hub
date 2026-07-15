import { Component, Inject, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api.service';
import { Client, ClientRequest } from '../../core/models';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [MatTableModule, MatButtonModule, MatIconModule],
  template: `
    <div class="head">
      <h1>Clients</h1>
      <button mat-flat-button color="primary" (click)="edit()"><mat-icon>add</mat-icon> New</button>
    </div>
    <table mat-table [dataSource]="rows()" class="mat-elevation-z1 full">
      <ng-container matColumnDef="name"><th mat-header-cell *matHeaderCellDef>Name</th><td mat-cell *matCellDef="let c"><b>{{ c.name }}</b></td></ng-container>
      <ng-container matColumnDef="industry"><th mat-header-cell *matHeaderCellDef>Industry</th><td mat-cell *matCellDef="let c">{{ c.industry }}</td></ng-container>
      <ng-container matColumnDef="contact"><th mat-header-cell *matHeaderCellDef>Contact</th><td mat-cell *matCellDef="let c">{{ c.contactName }}</td></ng-container>
      <ng-container matColumnDef="open"><th mat-header-cell *matHeaderCellDef>Open demands</th><td mat-cell *matCellDef="let c">{{ c.openDemands }}</td></ng-container>
      <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef></th>
        <td mat-cell *matCellDef="let c" class="actions">
          <button mat-icon-button (click)="edit(c)"><mat-icon>edit</mat-icon></button>
          <button mat-icon-button color="warn" (click)="remove(c)"><mat-icon>delete</mat-icon></button>
        </td></ng-container>
      <tr mat-header-row *matHeaderRowDef="cols"></tr>
      <tr mat-row *matRowDef="let row; columns: cols;"></tr>
    </table>
  `,
  styles: [`.head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; }
            h1 { margin: 0; } .full { width: 100%; } .actions { text-align: right; white-space: nowrap; }`],
})
export class ClientsComponent {
  private api = inject(ApiService);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);
  cols = ['name', 'industry', 'contact', 'open', 'actions'];
  rows = signal<Client[]>([]);

  constructor() { this.load(); }
  private load() { this.api.listClients().subscribe(r => this.rows.set(r)); }

  edit(c?: Client) {
    this.dialog.open(ClientDialog, { width: '420px', data: c ?? null }).afterClosed().subscribe(saved => {
      if (saved) { this.snack.open('Saved.', 'OK', { duration: 2000 }); this.load(); }
    });
  }
  remove(c: Client) {
    if (!confirm(`Delete ${c.name}? Its demands go too.`)) return;
    this.api.deleteClient(c.id).subscribe(() => { this.snack.open('Deleted.', 'OK', { duration: 2000 }); this.load(); });
  }
}

@Component({
  selector: 'app-client-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Edit' : 'New' }} client</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="col">
        <mat-form-field appearance="outline"><mat-label>Name</mat-label><input matInput formControlName="name"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Industry</mat-label><input matInput formControlName="industry"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Contact name</mat-label><input matInput formControlName="contactName"></mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: [`.col { display: flex; flex-direction: column; padding-top: 8px; } mat-form-field { width: 100%; }`],
})
export class ClientDialog {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private ref = inject(MatDialogRef<ClientDialog>);
  data = inject<Client | null>(MAT_DIALOG_DATA);

  form = this.fb.nonNullable.group({
    name: [this.data?.name ?? '', Validators.required],
    industry: [this.data?.industry ?? '', Validators.required],
    contactName: [this.data?.contactName ?? '', Validators.required],
  });

  save() {
    const req: ClientRequest = this.form.getRawValue();
    const obs = this.data ? this.api.updateClient(this.data.id, req) : this.api.createClient(req);
    obs.subscribe(() => this.ref.close(true));
  }
}
