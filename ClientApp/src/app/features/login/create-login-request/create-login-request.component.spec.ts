import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateLoginRequestComponent } from './create-login-request.component';

describe('CreateLoginRequestComponent', () => {
  let component: CreateLoginRequestComponent;
  let fixture: ComponentFixture<CreateLoginRequestComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [CreateLoginRequestComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CreateLoginRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
