import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TokenProvideComponent } from './token-provide.component';

describe('TokenProvideComponent', () => {
  let component: TokenProvideComponent;
  let fixture: ComponentFixture<TokenProvideComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [TokenProvideComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(TokenProvideComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
