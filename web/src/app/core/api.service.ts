import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from './config';
import {
  Allocation, AuditLog, Client, ClientRequest, Consultant, ConsultantRequest,
  DashboardSummary, Demand, DemandRequest, ExternalMatch, LlmProviders, Match,
  MatchingSettings, MatchingSettingsRequest
} from './models';

/** Thin typed wrapper over the REST API. One place that knows the endpoints. */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);

  dashboard(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${API_URL}/dashboard/summary`);
  }

  // Consultants
  listConsultants(filters?: { seniority?: string; availability?: string; skill?: string }): Observable<Consultant[]> {
    let params = new HttpParams();
    if (filters?.seniority) params = params.set('seniority', filters.seniority);
    if (filters?.availability) params = params.set('availability', filters.availability);
    if (filters?.skill) params = params.set('skill', filters.skill);
    return this.http.get<Consultant[]>(`${API_URL}/consultants`, { params });
  }
  getConsultant(id: number) { return this.http.get<Consultant>(`${API_URL}/consultants/${id}`); }
  createConsultant(r: ConsultantRequest) { return this.http.post<Consultant>(`${API_URL}/consultants`, r); }
  updateConsultant(id: number, r: ConsultantRequest) { return this.http.put<Consultant>(`${API_URL}/consultants/${id}`, r); }
  deleteConsultant(id: number) { return this.http.delete<void>(`${API_URL}/consultants/${id}`); }

  // Clients
  listClients() { return this.http.get<Client[]>(`${API_URL}/clients`); }
  createClient(r: ClientRequest) { return this.http.post<Client>(`${API_URL}/clients`, r); }
  updateClient(id: number, r: ClientRequest) { return this.http.put<Client>(`${API_URL}/clients/${id}`, r); }
  deleteClient(id: number) { return this.http.delete<void>(`${API_URL}/clients/${id}`); }

  // Demands
  listDemands() { return this.http.get<Demand[]>(`${API_URL}/demands`); }
  getDemand(id: number) { return this.http.get<Demand>(`${API_URL}/demands/${id}`); }
  createDemand(r: DemandRequest) { return this.http.post<Demand>(`${API_URL}/demands`, r); }
  updateDemand(id: number, r: DemandRequest) { return this.http.put<Demand>(`${API_URL}/demands/${id}`, r); }
  deleteDemand(id: number) { return this.http.delete<void>(`${API_URL}/demands/${id}`); }
  matches(id: number, provider?: string): Observable<Match[]> {
    let params = new HttpParams();
    if (provider) params = params.set('provider', provider);
    return this.http.get<Match[]>(`${API_URL}/demands/${id}/matches`, { params });
  }
  externalMatches(id: number, source = 'github', location?: string, limit = 6, provider?: string): Observable<ExternalMatch[]> {
    let params = new HttpParams().set('source', source).set('limit', limit);
    if (location) params = params.set('location', location);
    if (provider) params = params.set('provider', provider);
    return this.http.get<ExternalMatch[]>(`${API_URL}/demands/${id}/external-matches`, { params });
  }

  // LLM providers currently usable (for the live provider switch).
  llmProviders() { return this.http.get<LlmProviders>(`${API_URL}/llm/providers`); }

  // Allocations
  listAllocations() { return this.http.get<Allocation[]>(`${API_URL}/allocations`); }
  allocate(demandId: number, consultantId: number, startDate: string) {
    return this.http.post<Allocation>(`${API_URL}/allocations`, { demandId, consultantId, startDate });
  }
  endAllocation(id: number) { return this.http.post<Allocation>(`${API_URL}/allocations/${id}/end`, {}); }

  // Admin: matching settings + audit
  getMatchingSettings() { return this.http.get<MatchingSettings>(`${API_URL}/settings/matching`); }
  updateMatchingSettings(r: MatchingSettingsRequest) { return this.http.put<MatchingSettings>(`${API_URL}/settings/matching`, r); }
  listAudit() { return this.http.get<AuditLog[]>(`${API_URL}/audit`); }
}

