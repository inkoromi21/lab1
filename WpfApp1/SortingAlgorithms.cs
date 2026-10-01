namespace WpfApp1;

public static class SortingAlgorithms {
  public static SortExecution BubbleSort(List<double> values, bool isAscending, int unusedLimit, bool captureSnapshots, int unusedRandomSeed) {
    SortExecution execution = new SortExecution();

    if (values.Count == 1) {
      ++execution.IterationCount;
      if (captureSnapshots) {
        execution.SaveSnapshot(values);
      }

      return execution;
    }

    for (int passIndex = 0; passIndex < values.Count - 1; ++passIndex) {
      bool hasSwapped = false;

      for (int elementIndex = 0; elementIndex < values.Count - 1 - passIndex; ++elementIndex) {
        if (IsWrongOrder(values[elementIndex], values[elementIndex + 1], isAscending)) {
          Swap(values, elementIndex, elementIndex + 1);
          hasSwapped = true;
        }
      }

      ++execution.IterationCount;
      if (captureSnapshots) {
        execution.SaveSnapshot(values);
      }

      if (!hasSwapped) {
        break;
      }
    }

    return execution;
  }

  public static SortExecution InsertionSort(List<double> values, bool isAscending, int unusedLimit, bool captureSnapshots, int unusedRandomSeed) {
    SortExecution execution = new SortExecution();

    for (int currentIndex = 1; currentIndex < values.Count; ++currentIndex) {
      double currentValue = values[currentIndex];
      int insertIndex = currentIndex - 1;

      while (insertIndex >= 0 && IsWrongOrder(values[insertIndex], currentValue, isAscending)) {
        values[insertIndex + 1] = values[insertIndex];
        --insertIndex;
      }

      values[insertIndex + 1] = currentValue;
      ++execution.IterationCount;
      if (captureSnapshots) {
        execution.SaveSnapshot(values);
      }
    }

    return execution;
  }

  public static SortExecution ShakerSort(List<double> values, bool isAscending, int unusedLimit, bool captureSnapshots, int unusedRandomSeed) {
    SortExecution execution = new SortExecution();
    int leftIndex = 0;
    int rightIndex = values.Count - 1;

    if (values.Count == 1) {
      ++execution.IterationCount;
      if (captureSnapshots) {
        execution.SaveSnapshot(values);
      }

      return execution;
    }

    while (leftIndex < rightIndex) {
      bool hasSwapped = false;

      for (int elementIndex = leftIndex; elementIndex < rightIndex; ++elementIndex) {
        if (IsWrongOrder(values[elementIndex], values[elementIndex + 1], isAscending)) {
          Swap(values, elementIndex, elementIndex + 1);
          hasSwapped = true;
        }
      }

      --rightIndex;

      for (int elementIndex = rightIndex; elementIndex > leftIndex; --elementIndex) {
        if (IsWrongOrder(values[elementIndex - 1], values[elementIndex], isAscending)) {
          Swap(values, elementIndex - 1, elementIndex);
          hasSwapped = true;
        }
      }

      ++leftIndex;
      ++execution.IterationCount;
      if (captureSnapshots) {
        execution.SaveSnapshot(values);
      }

      if (!hasSwapped) {
        break;
      }
    }

    return execution;
  }

  public static SortExecution QuickSort(List<double> values, bool isAscending, int unusedLimit, bool captureSnapshots, int unusedRandomSeed) {
    SortExecution execution = new SortExecution();
    SortPart(values, 0, values.Count - 1, isAscending, captureSnapshots, execution);
    return execution;
  }

  public static SortExecution BogoSort(List<double> values, bool isAscending, int iterationLimit, bool captureSnapshots, int randomSeed) {
    SortExecution execution = new SortExecution();
    Random random = new Random(randomSeed);

    while (!IsSorted(values, isAscending) && execution.IterationCount < iterationLimit) {
      for (int elementIndex = values.Count - 1; elementIndex > 0; --elementIndex) {
        int randomIndex = random.Next(elementIndex + 1);
        Swap(values, elementIndex, randomIndex);
      }

      ++execution.IterationCount;
      if (captureSnapshots) {
        execution.SaveSnapshot(values);
      }
    }

    execution.IsCompleted = IsSorted(values, isAscending);
    return execution;
  }

  private static void SortPart(List<double> values, int leftIndex, int rightIndex, bool isAscending, bool captureSnapshots, SortExecution execution) {
    if (leftIndex >= rightIndex) {
      return;
    }

    double pivotValue = values[(leftIndex + rightIndex) / 2];
    int currentLeftIndex = leftIndex;
    int currentRightIndex = rightIndex;
    ++execution.IterationCount;

    while (currentLeftIndex <= currentRightIndex) {
      while (IsBefore(values[currentLeftIndex], pivotValue, isAscending)) {
        ++currentLeftIndex;
      }

      while (IsBefore(pivotValue, values[currentRightIndex], isAscending)) {
        --currentRightIndex;
      }

      if (currentLeftIndex <= currentRightIndex) {
        Swap(values, currentLeftIndex, currentRightIndex);
        ++currentLeftIndex;
        --currentRightIndex;
      }
    }

    if (captureSnapshots) {
      execution.SaveSnapshot(values);
    }

    SortPart(values, leftIndex, currentRightIndex, isAscending, captureSnapshots, execution);
    SortPart(values, currentLeftIndex, rightIndex, isAscending, captureSnapshots, execution);
  }

  private static bool IsWrongOrder(double leftValue, double rightValue, bool isAscending) {
    return isAscending ? leftValue > rightValue : leftValue < rightValue;
  }

  private static bool IsBefore(double leftValue, double rightValue, bool isAscending) {
    return isAscending ? leftValue < rightValue : leftValue > rightValue;
  }

  private static bool IsSorted(List<double> values, bool isAscending) {
    for (int elementIndex = 0; elementIndex < values.Count - 1; ++elementIndex) {
      if (IsWrongOrder(values[elementIndex], values[elementIndex + 1], isAscending)) {
        return false;
      }
    }

    return true;
  }

  private static void Swap(List<double> values, int firstIndex, int secondIndex) {
    double temporaryValue = values[firstIndex];
    values[firstIndex] = values[secondIndex];
    values[secondIndex] = temporaryValue;
  }
}
