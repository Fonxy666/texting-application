import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateEmailVerificationRequestComponent } from './create-email-verification-request.component';

describe('CreateEmailVerificationRequestComponent', () => {
  let component: CreateEmailVerificationRequestComponent;
  let fixture: ComponentFixture<CreateEmailVerificationRequestComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [CreateEmailVerificationRequestComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CreateEmailVerificationRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
