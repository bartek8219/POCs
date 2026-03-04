import torch
import torch.nn as nn
import torch.optim as optim


# Model definition
class SimpleNN(nn.Module):
    def __init__(self):
        super(SimpleNN, self).__init__()
        self.fc1 = nn.Linear(2, 4)
        self.fc2 = nn.Linear(4, 8)
        self.fc3 = nn.Linear(8, 1)

    def forward(self, x):
        x = torch.relu(self.fc1(x))
        x = torch.relu(self.fc2(x))
        # Sigmoid activation for output (binary classification: either 0 or 1)
        x = torch.sigmoid(self.fc3(x))
        return x


model = SimpleNN()

# Using 20 samples, data is synthetic, creating two easily separable groups.
X_class_0 = torch.randn(10, 2) * 0.05 + 0.1
X_class_1 = torch.randn(10, 2) * 0.05 + 0.9

# Merging both classes into one dataset: 20 samples, 2 features each
X = torch.cat((X_class_0, X_class_1), dim=0).float()

# Joining 20 targets, 1 target each
y = torch.cat(
    (
        # Target: 10 zeroes for Class 0 and 10 ones for Class 1
        torch.zeros(10, 1),
        torch.ones(10, 1),
    ),
    dim=0,
).float()

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
