import torch
import torch.nn as nn
import torch.optim as optim


# Model definition (single input feature)
class SimpleNN(nn.Module):
    def __init__(self):
        super(SimpleNN, self).__init__()
        self.fc1 = nn.Linear(1, 4)
        self.fc2 = nn.Linear(4, 1)

    def forward(self, x):
        x = torch.relu(self.fc1(x))
        # Sigmoid activation for output (binary classification: either 0 or 1)
        x = torch.sigmoid(self.fc2(x))
        return x


torch.manual_seed(42)
model = SimpleNN()

# Dataset: numbers 1..20 as one feature
X = torch.arange(1, 21, dtype=torch.float32).unsqueeze(1)
# Scale feature for more stable optimization
X = X / 20.0

# Targets: even -> 1, odd -> 0
y = ((torch.arange(1, 21) % 2) == 0).float().unsqueeze(1)

# Criterion - Binary Cross Entropy Loss
criterion = nn.BCELoss()
# Adam
optimizer = optim.Adam(model.parameters(), lr=0.01)

NUM_EPOCHS = 100

# Training loop
print(f"Rozpoczynanie treningu przez {NUM_EPOCHS} epok...")

for epoch in range(NUM_EPOCHS):
    # Gradient zeroing
    optimizer.zero_grad()

    # Forward pass
    output = model(X)
    loss = criterion(output, y)

    # Backpropagation (calculating gradients)
    loss.backward()

    # Update weights
    optimizer.step()

    if (epoch + 1) % 5 == 0:
        print(f"Epoka [{epoch+1}/{NUM_EPOCHS}], Strata: {loss.item():.4f}")

print("Trening zakonczony.")

# Short final check on training set
with torch.no_grad():
    preds = (model(X) >= 0.5).float()
    acc = (preds == y).float().mean().item()
print(f"Dokladnosc na zbiorze 1..20: {acc:.4f}")
