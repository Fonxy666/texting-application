import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GeneratePasswordChangeRequestComponent } from './generate-password-change-request.component';

describe('GeneratePasswordChangeRequestComponent', () => {
  let component: GeneratePasswordChangeRequestComponent;
  let fixture: ComponentFixture<GeneratePasswordChangeRequestComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GeneratePasswordChangeRequestComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GeneratePasswordChangeRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
