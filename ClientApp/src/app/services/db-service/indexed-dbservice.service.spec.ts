import { TestBed } from '@angular/core/testing';

import { IndexedDBServiceService } from './indexed-dbservice.service';

describe('IndexedDBServiceService', () => {
  let service: IndexedDBServiceService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(IndexedDBServiceService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
