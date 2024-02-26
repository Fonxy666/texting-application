import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProvideLoginAuthTokenComponent } from './provide-login-auth-token.component';

describe('ProvideLoginAuthTokenComponent', () => {
  let component: ProvideLoginAuthTokenComponent;
  let fixture: ComponentFixture<ProvideLoginAuthTokenComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ProvideLoginAuthTokenComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ProvideLoginAuthTokenComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
