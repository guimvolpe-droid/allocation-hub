export type Seniority = 'Junior' | 'Mid' | 'Senior' | 'Lead';
export type Availability = 'Available' | 'Allocated' | 'Unavailable';
export type DemandStatus = 'Open' | 'Allocated' | 'Closed';
export type AllocationStatus = 'Active' | 'Ended';

export const SENIORITIES: Seniority[] = ['Junior', 'Mid', 'Senior', 'Lead'];
export const AVAILABILITIES: Availability[] = ['Available', 'Allocated', 'Unavailable'];

export interface User { id: number; name: string; email: string; role: string; }
export interface AuthResponse { token: string; user: User; }

export interface Consultant {
  id: number; name: string; email: string; seniority: Seniority; location: string;
  availability: Availability; hourlyRate: number; skills: string[];
}
export interface ConsultantRequest {
  name: string; email: string; seniority: Seniority; location: string;
  availability: Availability; hourlyRate: number; skills: string[];
}

export interface Client { id: number; name: string; industry: string; contactName: string; openDemands: number; }
export interface ClientRequest { name: string; industry: string; contactName: string; }

export interface Demand {
  id: number; clientId: number; clientName: string; title: string; description: string;
  requiredSeniority: Seniority; requiredSkills: string[]; status: DemandStatus;
}
export interface DemandRequest {
  clientId: number; title: string; description: string;
  requiredSeniority: Seniority; requiredSkills: string[];
}

export interface Allocation {
  id: number; demandId: number; demandTitle: string; consultantId: number; consultantName: string;
  startDate: string; endDate: string | null; status: AllocationStatus;
}

export interface Match {
  consultantId: number; name: string; seniority: Seniority; availability: Availability;
  score: number; matchedSkills: string[]; missingSkills: string[]; explanation: string;
}

export interface MatchingSettings {
  availabilityWeight: number; skillWeight: number; seniorityWeight: number; allocatedPenalty: number;
  updatedAt: string; updatedBy: string;
}
export interface MatchingSettingsRequest {
  availabilityWeight: number; skillWeight: number; seniorityWeight: number; allocatedPenalty: number;
}
export interface AuditLog {
  id: number; actor: string; action: string; entity: string; details: string; createdAt: string;
}

export interface DashboardSummary {
  totalConsultants: number; availableConsultants: number; allocatedConsultants: number;
  openDemands: number; topOpenDemands: Demand[]; availableNow: Consultant[];
}
