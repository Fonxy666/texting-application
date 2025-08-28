import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateRegistrationRequestComponent } from './create-registration-request.component';

describe('CreateRegistrationRequestComponent', () => {
  let component: CreateRegistrationRequestComponent;
  let fixture: ComponentFixture<CreateRegistrationRequestComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [CreateRegistrationRequestComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CreateRegistrationRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
