import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api.service';
import { Demand, ExternalMatch, Match } from '../../core/models';

@Component({
  selector: 'app-match',
  standalone: true,
  imports: [
    RouterLink, FormsModule, MatCardModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatProgressBarModule, MatProgressSpinnerModule, MatFormFieldModule, MatInputModule, MatSelectModule
  ],
  template: `
    <a mat-button routerLink="/demands"><mat-icon>arrow_back</mat-icon> Demands</a>

    @if (demand(); as d) {
      <mat-card class="head">
        <h1>{{ d.title }}</h1>
        <p class="sub">{{ d.clientName }} · needs <b>{{ d.requiredSeniority }}</b> · status {{ d.status }}</p>
        <mat-chip-set>@for (s of d.requiredSkills; track s) { <mat-chip color="primary" highlighted>{{ s }}</mat-chip> }</mat-chip-set>
      </mat-card>
    }

    <h2>Recommended consultants</h2>
    <p class="explain">Ranked by a transparent score: availability, seniority fit and matched skills.</p>

    @if (llmAvailable()) {
      <div class="llm-switch">
        <mat-form-field appearance="outline" class="prov">
          <mat-label>Explanation by</mat-label>
          <mat-select [(value)]="provider" (selectionChange)="onProviderChange()">
            <mat-option value="">Deterministic (offline)</mat-option>
            @for (p of llmProviders(); track p) { <mat-option [value]="p">{{ p }}</mat-option> }
          </mat-select>
        </mat-form-field>
        @if (busy() && provider) {
          <span class="hint working"><mat-spinner diameter="16" class="btn-spin"></mat-spinner>
            Rewriting explanations with {{ provider }}…</span>
        } @else {
          <span class="hint">Switch the LLM live · guarded, falls back to deterministic on failure</span>
        }
      </div>
    }

    @if (busy()) { <mat-progress-bar mode="indeterminate"></mat-progress-bar> }

    @for (m of matches(); track m.consultantId; let i = $index) {
      <mat-card class="match" [class.top]="i === 0">
        <div class="score" [class.good]="m.score >= 70" [class.mid]="m.score >= 40 && m.score < 70">{{ m.score }}</div>
        <div class="body">
          <div class="who">{{ m.name }} <span class="tag">{{ m.seniority }} · {{ m.availability }}</span></div>
          <div class="skills">
            @for (s of m.matchedSkills; track s) { <mat-chip class="ok" highlighted>{{ s }}</mat-chip> }
            @for (s of m.missingSkills; track s) { <mat-chip class="miss">{{ s }}</mat-chip> }
          </div>
          <div class="why">{{ m.explanation }}</div>
        </div>
        <button mat-flat-button color="primary" [disabled]="m.availability === 'Allocated' || busy()"
                (click)="allocate(m)">
          <mat-icon>person_add</mat-icon> Allocate
        </button>
      </mat-card>
    } @empty { @if (!busy()) { <p class="muted">No consultants to rank.</p> } }

    <!-- ── The "spice": rank REAL developers sourced live from the GitHub API ── -->
    <div class="ext-head">
      <h2>Source real candidates <span class="src">· GitHub</span></h2>
      <p class="explain">
        LinkedIn has no compliant people-search API — for a developer-staffing tool the real, official
        source is GitHub. Same matching rule, real public profiles.
      </p>
    </div>

    <div class="ext-controls">
      <mat-form-field appearance="outline" class="loc">
        <mat-label>Location (optional)</mat-label>
        <input matInput [(ngModel)]="location" placeholder="e.g. Brazil" (keyup.enter)="searchGitHub()">
      </mat-form-field>
      <button mat-flat-button class="lv-accent-btn" [disabled]="extBusy()" (click)="searchGitHub()">
        @if (extBusy()) {
          <mat-spinner diameter="18" class="btn-spin"></mat-spinner> Searching GitHub…
        } @else {
          <mat-icon>travel_explore</mat-icon> Search GitHub
        }
      </button>
    </div>

    @if (extBusy()) {
      <mat-progress-bar mode="indeterminate"></mat-progress-bar>
      <p class="loading-note">
        Querying the GitHub API live — reading profiles and repositories to infer skills. Takes a few seconds.
      </p>
    }
    @if (extError(); as e) { <p class="err">{{ e }}</p> }

    @for (m of externalMatches(); track m.externalId; let i = $index) {
      <mat-card class="match ext" [class.top]="i === 0">
        <div class="score" [class.good]="m.score >= 70" [class.mid]="m.score >= 40 && m.score < 70">{{ m.score }}</div>
        @if (m.avatarUrl) { <img class="avatar" [src]="m.avatarUrl" alt=""> }
        <div class="body">
          <div class="who">
            <a [href]="m.profileUrl" target="_blank" rel="noopener">{{ m.name }}</a>
            <span class="tag">{{ m.seniority }} · {{ m.source }}</span>
          </div>
          @if (m.headline) { <div class="headline">{{ m.headline }}</div> }
          <div class="skills">
            @for (s of m.matchedSkills; track s) { <mat-chip class="ok" highlighted>{{ s }}</mat-chip> }
            @for (s of m.missingSkills; track s) { <mat-chip class="miss">{{ s }}</mat-chip> }
          </div>
          <div class="why">{{ m.explanation }}</div>
        </div>
        <div class="ext-actions">
          <a mat-stroked-button [href]="m.profileUrl" target="_blank" rel="noopener">
            <mat-icon>open_in_new</mat-icon> GitHub
          </a>
          @if (imported().has(m.externalId)) {
            <span class="pill-imported"><mat-icon inline>check_circle</mat-icon> Imported</span>
          } @else {
            <button mat-flat-button class="lv-accent-btn" [disabled]="importing() === m.externalId"
                    (click)="importCandidate(m)">
              @if (importing() === m.externalId) {
                <mat-spinner diameter="18" class="btn-spin"></mat-spinner> Saving…
              } @else {
                <mat-icon>person_add</mat-icon> Import
              }
            </button>
          }
        </div>
      </mat-card>
    } @empty { @if (extSearched() && !extBusy() && !extError()) { <p class="muted">No GitHub candidates found for these skills.</p> } }
  `,
  styles: [`
    .head { margin: 12px 0 20px; padding: 16px; }
    .head h1 { margin: 0 0 4px; } .sub { color: #666; margin: 0 0 10px; }
    h2 { margin: 8px 0 2px; } .explain { color: #777; margin: 0 0 14px; font-size: .88rem; max-width: 620px; }
    .match { display: flex; align-items: center; gap: 16px; padding: 14px 16px; margin-bottom: 10px; }
    .match.top { outline: 2px solid var(--lv-primary); }
    .score { font-size: 1.7rem; font-weight: 700; width: 56px; text-align: center; color: var(--lv-err); }
    .score.mid { color: var(--lv-warn); } .score.good { color: var(--lv-ok); }
    .avatar { width: 44px; height: 44px; border-radius: 50%; }
    .body { flex: 1 1 auto; }
    .who { font-weight: 600; } .who a { color: var(--lv-primary); text-decoration: none; } .who a:hover { text-decoration: underline; }
    .tag { color: #888; font-weight: 400; font-size: .85rem; }
    .headline { color: #666; font-size: .85rem; margin: 2px 0; }
    .skills { margin: 6px 0; display: flex; flex-wrap: wrap; gap: 4px; }
    mat-chip.ok { background: var(--lv-ok-tint) !important; } mat-chip.miss { background: var(--lv-err-tint) !important; opacity: .8; }
    .why { color: #555; font-size: .88rem; }
    .muted { color: #999; }
    .err { color: var(--lv-err); font-size: .9rem; margin: 6px 0; }
    .llm-switch { display: flex; align-items: center; gap: 12px; margin: 4px 0 8px; }
    .llm-switch .prov { width: 220px; }
    .llm-switch .hint { color: var(--lv-accent-ink); font-size: .82rem; }
    .llm-switch .hint.working { display: inline-flex; align-items: center; gap: 6px; font-weight: 600; }
    .btn-spin { display: inline-block; margin-right: 6px; vertical-align: middle; }
    .btn-spin ::ng-deep circle { stroke: currentColor; }
    .loading-note { color: var(--lv-accent-ink); font-size: .82rem; margin: 8px 0 4px; }
    .ext-head { margin-top: 30px; border-top: 1px solid #eee; padding-top: 14px; }
    .ext-head .src { color: var(--lv-accent-ink); font-weight: 600; }
    .ext-controls { display: flex; gap: 12px; align-items: center; margin-bottom: 12px; }
    .ext-controls .loc { width: 220px; }
    .match.ext { background: var(--lv-primary-wash); }
    .ext-actions { display: flex; flex-direction: column; gap: 6px; align-items: stretch; }
    .pill-imported { display: inline-flex; align-items: center; gap: 4px; background: var(--lv-accent-tint);
      color: var(--lv-accent-ink); font-size: .8rem; font-weight: 600; padding: 4px 10px; border-radius: 12px; justify-content: center; }
  `],
})
export class MatchComponent {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snack = inject(MatSnackBar);

  demand = signal<Demand | null>(null);
  matches = signal<Match[]>([]);
  busy = signal(false);

  // Multi-LLM live switch. '' = deterministic (offline) baseline.
  provider = '';
  llmProviders = signal<string[]>([]);
  llmAvailable = signal(false);

  // External (GitHub) sourcing state
  location = '';
  externalMatches = signal<ExternalMatch[]>([]);
  extBusy = signal(false);
  extError = signal<string | null>(null);
  extSearched = signal(false);
  imported = signal<Set<string>>(new Set());
  importing = signal<string | null>(null); // externalId being saved right now

  private id = Number(this.route.snapshot.paramMap.get('id'));

  constructor() {
    this.api.getDemand(this.id).subscribe(d => this.demand.set(d));
    this.api.llmProviders().subscribe(p => {
      this.llmProviders.set(p.providers);
      this.llmAvailable.set(p.available);
    });
    this.load();
  }

  onProviderChange(): void {
    // Re-explain the internal ranking only. Deliberately NOT re-running the GitHub search here:
    // each search costs ~13 API calls and would silently burn the rate-limit budget on a dropdown flip.
    this.load();
  }

  private load(): void {
    this.busy.set(true);
    this.api.matches(this.id, this.provider || undefined).subscribe({
      next: m => { this.matches.set(m); this.busy.set(false); },
      error: () => {
        this.busy.set(false);
        this.snack.open('Could not load matches — API unavailable?', 'OK', { duration: 4000 });
      },
    });
  }

  searchGitHub(): void {
    this.extBusy.set(true);
    this.extError.set(null);
    this.extSearched.set(true);
    this.api.externalMatches(this.id, 'github', this.location.trim() || undefined, 6, this.provider || undefined).subscribe({
      next: m => { this.externalMatches.set(m); this.extBusy.set(false); },
      error: err => {
        this.externalMatches.set([]);
        this.extError.set(err?.error?.message ?? 'Could not reach the GitHub source.');
        this.extBusy.set(false);
      },
    });
  }

  importCandidate(m: ExternalMatch): void {
    this.importing.set(m.externalId);
    this.api.importCandidate(m).subscribe({
      next: c => {
        const s = new Set(this.imported()); s.add(m.externalId); this.imported.set(s);
        this.importing.set(null);
        this.snack.open(`${m.name} imported as consultant #${c.id}.`, 'OK', { duration: 3500 });
        this.load(); // the real dev now shows up in the internal ranking, persisted in the DB
      },
      error: () => {
        this.importing.set(null);
        this.snack.open('Could not import candidate.', 'OK', { duration: 3000 });
      },
    });
  }

  allocate(m: Match): void {
    this.busy.set(true);
    const today = new Date().toISOString().slice(0, 10);
    this.api.allocate(this.id, m.consultantId, today).subscribe({
      next: () => {
        this.snack.open(`${m.name} allocated to this demand.`, 'OK', { duration: 3000 });
        this.router.navigate(['/allocations']);
      },
      error: () => { this.snack.open('Could not allocate.', 'OK', { duration: 3000 }); this.busy.set(false); },
    });
  }
}
