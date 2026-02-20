import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface OwnershipEntity {
    id: string;
    name: string;
    entityType: string;
    cik: string | null;
    firstSeen: string;
    lastSeen: string;
    createdAt: string;
}

export interface OwnershipEntityCompanyRole {
    id: string;
    entityId: string;
    companyTicker: string;
    companyCik: string;
    relationshipType: string;
    title: string | null;
    isActive: boolean;
    firstSeen: string;
    lastSeen: string;
}

export interface OwnershipEvent {
    id: string;
    entityId: string;
    companyTicker: string;
    companyCik: string;
    filingId: string | null;
    eventType: string;
    transactionType: string | null;
    sharesBefore: number | null;
    sharesTransacted: number | null;
    sharesAfter: number;
    percentOfClass: number | null;
    pricePerShare: number | null;
    totalValue: number | null;
    transactionDate: string;
    filingDate: string;
    isDirect: boolean;
    ownershipNature: string | null;
    createdAt: string;
}

export interface OwnershipSummary {
    entity: OwnershipEntity;
    roles: OwnershipEntityCompanyRole[];
    currentShares: number;
    percentOfClass: number | null;
    lastUpdated: string;
}

export interface CreateEntityRequest {
    name: string;
    entityType: string;
    cik: string | null;
}

export interface CreateRoleRequest {
    entityId: string;
    companyTicker: string;
    companyCik: string;
    relationshipType: string;
    title: string | null;
}

export interface CreateOwnershipEventRequest {
    entityId: string;
    companyTicker: string;
    companyCik: string;
    filingId: string | null;
    eventType: string;
    transactionType: string | null;
    sharesBefore: number | null;
    sharesTransacted: number | null;
    sharesAfter: number;
    percentOfClass: number | null;
    pricePerShare: number | null;
    totalValue: number | null;
    transactionDate: string;
    filingDate: string;
    isDirect: boolean;
    ownershipNature: string | null;
}

@Injectable({providedIn: 'root'})
export class OwnershipService {
    private http = inject(HttpClient);

    // Helper function to display entity type with friendly names
    static getEntityTypeDisplay(entityType: string): string {
        const typeMap: { [key: string]: string } = {
            'IA': 'Investment Adviser (IA)',
            'IC': 'Investment Company (IC)',
            'BD': 'Broker Dealer (BD)',
            'IN': 'Individual (IN)',
            'HC': 'Holding Company (HC)',
            'FI': 'Financial Institution (FI)',
            'EP': 'Employee (EP)',
            'OO': 'Officer/Director (OO)',
            'individual': 'Individual',
            'other': 'Other'
        };
        return typeMap[entityType] || entityType;
    }

    // Get ownership by ticker
    getOwnershipByTicker(ticker: string): Observable<OwnershipSummary[]> {
        return this.http.get<OwnershipSummary[]>(`/api/ownership/ticker/${ticker}`);
    }

    getOwnershipTimeline(ticker: string, days: number = 1825): Observable<OwnershipEvent[]> {
        return this.http.get<OwnershipEvent[]>(`/api/ownership/ticker/${ticker}/timeline?days=${days}`);
    }

    getEventsByTicker(ticker: string): Observable<OwnershipEvent[]> {
        return this.http.get<OwnershipEvent[]>(`/api/ownership/ticker/${ticker}/events`);
    }

    getRolesByTicker(ticker: string): Observable<OwnershipEntityCompanyRole[]> {
        return this.http.get<OwnershipEntityCompanyRole[]>(`/api/ownership/ticker/${ticker}/roles`);
    }

    // Get entity information
    getEntity(entityId: string): Observable<OwnershipEntity> {
        return this.http.get<OwnershipEntity>(`/api/ownership/entity/${entityId}`);
    }

    getEntityRoles(entityId: string): Observable<OwnershipEntityCompanyRole[]> {
        return this.http.get<OwnershipEntityCompanyRole[]>(`/api/ownership/entity/${entityId}/roles`);
    }

    getEventsByEntity(entityId: string): Observable<OwnershipEvent[]> {
        return this.http.get<OwnershipEvent[]>(`/api/ownership/entity/${entityId}/events`);
    }

    // Search entities
    searchEntities(name: string): Observable<OwnershipEntity[]> {
        return this.http.get<OwnershipEntity[]>(`/api/ownership/entities/search?name=${encodeURIComponent(name)}`);
    }

    // Get recent ownership timelines across all companies
    getRecentTimelines(limit: number = 50): Observable<OwnershipEvent[]> {
        return this.http.get<OwnershipEvent[]>(`/api/ownership/timelines/recent?limit=${limit}`);
    }

    // Get multiple entities by their IDs in a single request
    getEntitiesByIds(ids: string[]): Observable<OwnershipEntity[]> {
        return this.http.post<OwnershipEntity[]>('/api/ownership/entities/batch', { ids });
    }

    // Create/update operations
    createEntity(request: CreateEntityRequest): Observable<{id: string}> {
        return this.http.post<{id: string}>('/api/ownership/entity', request);
    }

    createRole(request: CreateRoleRequest): Observable<{id: string}> {
        return this.http.post<{id: string}>('/api/ownership/role', request);
    }

    createOwnershipEvent(request: CreateOwnershipEventRequest): Observable<{id: string}> {
        return this.http.post<{id: string}>('/api/ownership/event', request);
    }
}
