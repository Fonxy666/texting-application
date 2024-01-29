import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GenerateEmailChangeRequestComponent } from './generate-email-change-request.component';

describe('GenerateEmailChangeRequestComponent', () => {
  let component: GenerateEmailChangeRequestComponent;
  let fixture: ComponentFixture<GenerateEmailChangeRequestComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GenerateEmailChangeRequestComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GenerateEmailChangeRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
